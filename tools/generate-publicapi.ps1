#requires -Version 5.1
<#
.SYNOPSIS
    (Re)generate PublicAPI baseline files for every shipped Akavache library,
    across each target framework that builds on this machine.

.DESCRIPTION
    PublicApiSharp.Analyzers (PAS0001-PAS0005) tracks one baseline per target framework:

        <Project>/PublicAPI/<tfm>/PublicAPI.txt

    The file is nested C# describing the assembly's current surface — there is no
    shipped/unshipped split and nothing to promote. This script seeds an empty baseline so
    the analyzer reports the whole surface as PAS0001, then lets `dotnet format analyzers`
    apply the baseline fix, which writes the file.

    Only projects with MSBuild property TrackPublicApi=true are processed; tests,
    benchmarks, samples and compat opt out centrally in src/Directory.Build.props.

    Both sides of the lean/.Reactive seam are tracked, so the same source change shows up
    as a diff against Akavache.X and Akavache.X.Reactive alike.

    Each (project, TFM) pair is independent — `dotnet format` builds an in-memory
    MSBuildWorkspace and only writes its own PublicAPI/<tfm>/PublicAPI.txt — so the pairs
    run in parallel (PowerShell 7+ runspaces; falls back to sequential on 5.1). Override
    the width with -Jobs <n> or $env:JOBS.

    Run on Windows to generate the Windows-desktop and (with the relevant workloads)
    Apple/Android target frameworks. Use the bash sibling (generate-publicapi.sh) on
    Linux/macOS. A TFM whose workload/SDK is missing is reported as failed (its previous
    baseline is restored) rather than aborting the whole run.

.PARAMETER Filter
    Optional substring; only projects whose path contains it are processed.

.PARAMETER Jobs
    Maximum number of (project, TFM) pairs to generate concurrently.

.EXAMPLE
    ./tools/generate-publicapi.ps1
    Generates baselines for all tracked libraries across all buildable TFMs.

.EXAMPLE
    ./tools/generate-publicapi.ps1 -Filter Sqlite3 -Jobs 4
    Only projects whose path contains 'Sqlite3', 4 at a time.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Filter = '',
    [int]$Jobs = 0
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = (Resolve-Path (Join-Path $scriptDir '..' 'src')).Path
Set-Location $srcDir

# MSBuild properties that `dotnet format` cannot accept via -p:; pass through the env
# (also inherited by the parallel runspaces, which share this process).
$env:EnableWindowsTargeting = 'true'
$env:CheckEolTargetFramework = 'false'
if (-not $env:MinVerVersionOverride) { $env:MinVerVersionOverride = '255.255.255-dev' }

if ($Jobs -le 0) {
    $Jobs = if ($env:JOBS) { [int]$env:JOBS } else { [Math]::Min([Environment]::ProcessorCount, 8) }
}

# PAS0001 (surface missing from the baseline) and PAS0003 (surface differs) are the two
# fixable rules; both resolve to the same "write the baseline" fix.
$diags = @('PAS0001', 'PAS0003')

Write-Host 'PublicAPI baseline generation'
Write-Host "  src        : $srcDir"
Write-Host "  filter     : $(if ($Filter) { $Filter } else { '<none>' })"
Write-Host "  diagnostics: $($diags -join ' ')"
Write-Host "  MinVer     : $($env:MinVerVersionOverride)"
Write-Host "  jobs       : $Jobs"
Write-Host ''

function Get-MsBuildProperty {
    param([string]$Project, [string]$Name)
    $value = & dotnet msbuild $Project "-getProperty:$Name" -nologo 2>$null
    if ($LASTEXITCODE -ne 0 -or $null -eq $value) { return '' }
    return ($value | Out-String).Trim()
}

# Regenerate one (project, TFM) baseline. Returns a result object; also defined inside the
# parallel block below via its source text.
function Invoke-PublicApiOne {
    param($Item, [string[]]$Diags)
    $proj = $Item.Proj
    $tfm = $Item.Tfm
    # Back up any existing baseline so a build failure (a TFM whose workload this platform
    # lacks) restores it instead of wiping it.
    $baselineBak = if (Test-Path $Item.Baseline) { (Get-Content -Raw $Item.Baseline) -replace "`r`n", "`n" } else { $null }
    # An empty baseline makes the analyzer report the entire current surface as PAS0001,
    # which the fix then writes back in full. The file has to exist: with no baseline at
    # all the analyzer reports PAS0004, which has no fix.
    [IO.File]::WriteAllText($Item.Baseline, '')
    & dotnet format analyzers $proj -f $tfm --diagnostics $Diags --severity info -v quiet
    if ($LASTEXITCODE -eq 0) {
        # Normalize to LF so the baselines match the bash sibling's output byte-for-byte.
        $written = (Get-Content -Raw $Item.Baseline) -replace "`r`n", "`n"
        [IO.File]::WriteAllText($Item.Baseline, $written)
        Write-Host "OK   [$tfm] $proj"
        return [pscustomobject]@{ Ok = $true }
    }
    # Restore the prior baseline (if any) so nothing is wiped for a TFM we can't build here.
    if ($null -ne $baselineBak) {
        [IO.File]::WriteAllText($Item.Baseline, $baselineBak)
    }
    else {
        Remove-Item -Force -ErrorAction SilentlyContinue $Item.Baseline
    }
    Write-Host "FAIL [$tfm] $proj (missing workload/SDK for this platform?)"
    return [pscustomobject]@{ Ok = $false }
}

$projects = Get-ChildItem -Path . -Recurse -Filter '*.csproj' |
    Where-Object {
        $p = $_.FullName -replace '\\', '/'
        $p -notmatch '/tests/' -and $p -notmatch '/benchmarks/' -and
        $p -notmatch '/samples/' -and $p -notmatch '/compat/' -and
        $p -notmatch '_wpftmp\.csproj$'
    } |
    Sort-Object FullName

# Collect (project, TFM) work items; the worker seeds and generates each one.
$items = [System.Collections.Generic.List[object]]::new()
$restoreSet = [System.Collections.Generic.List[string]]::new()
$skipped = 0

foreach ($projItem in $projects) {
    $proj = $projItem.FullName
    # Match the filter against a slash-normalized path so a forward-slash filter works on Windows too.
    if ($Filter -and (($proj -replace '\\', '/') -notlike "*$($Filter -replace '\\', '/')*")) { continue }

    $track = Get-MsBuildProperty -Project $proj -Name 'TrackPublicApi'
    if ($track -ne 'true') {
        Write-Host "skip  (TrackPublicApi != true): $proj"
        $skipped++
        continue
    }

    $tfms = Get-MsBuildProperty -Project $proj -Name 'TargetFrameworks'
    if (-not $tfms) { $tfms = Get-MsBuildProperty -Project $proj -Name 'TargetFramework' }
    if (-not $tfms) {
        Write-Host "skip  (no TargetFramework(s)): $proj"
        $skipped++
        continue
    }

    $projDir = Split-Path -Parent $proj
    Write-Host "queue $proj"
    Write-Host "    TFMs: $tfms"
    $restoreSet.Add($proj)

    foreach ($tfm in ($tfms -split ';')) {
        $tfm = $tfm.Trim()
        if (-not $tfm) { continue }

        $apiDir = Join-Path $projDir (Join-Path 'PublicAPI' $tfm)
        New-Item -ItemType Directory -Force -Path $apiDir | Out-Null

        $baseline = Join-Path $apiDir 'PublicAPI.txt'
        $items.Add([pscustomobject]@{ Proj = $proj; Tfm = $tfm; Baseline = $baseline })
    }
}
Write-Host ''

if ($items.Count -eq 0) {
    Write-Host "Nothing to generate. projects skipped: $skipped"
    return
}

# Restore once per project so the parallel workers never race on restore (they each load
# a read-only workspace afterwards).
Write-Host "Restoring $($restoreSet.Count) project(s)..."
foreach ($proj in $restoreSet) {
    & dotnet restore $proj -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Host "    WARN: restore reported issues for $proj" }
}
Write-Host ''

Write-Host "Generating $($items.Count) (project, TFM) baseline(s) across $Jobs job(s)..."
if ($PSVersionTable.PSVersion.Major -ge 7 -and $Jobs -gt 1) {
    $funcDef = ${function:Invoke-PublicApiOne}.ToString()
    $results = $items | ForEach-Object -ThrottleLimit $Jobs -Parallel {
        ${function:Invoke-PublicApiOne} = $using:funcDef
        Invoke-PublicApiOne -Item $_ -Diags $using:diags
    }
}
else {
    if ($Jobs -gt 1) { Write-Host '  (PowerShell 5.1: running sequentially — use pwsh 7+ for parallelism)' }
    $results = foreach ($it in $items) { Invoke-PublicApiOne -Item $it -Diags $diags }
}
Write-Host ''

$generated = @($results | Where-Object { $_.Ok }).Count
$failed = @($results | Where-Object { -not $_.Ok }).Count

Write-Host "Done. generated: $generated TFM baseline(s), failed: $failed, projects skipped: $skipped"
if ($failed -ne 0) { exit 1 }
