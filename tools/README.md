# tools

Maintenance scripts for the Akavache repository.

## generate-publicapi

Regenerates the **PublicAPI baseline files** consumed by `PublicApiSharp.Analyzers`
(`PAS0001`-`PAS0005`).

Each shipped library tracks its public surface per target framework in:

```
src/<Project>/PublicAPI/<tfm>/PublicAPI.txt
```

The baseline is nested C# describing what the assembly exposes right now, so a reviewer
reads an API change the way they read code:

```csharp
namespace Akavache;

public static class AkavacheBuilderExtensions
{
    extension(Akavache.IAkavacheBuilder builder)
    {
        public Akavache.IAkavacheBuilder WithInMemory() { }
    }
}
```

There is no shipped/unshipped split and no promotion step — the baseline is updated in the
same commit that changes the API. When the surface and the file disagree the build fails
with `PAS0001` (surface not in the baseline), `PAS0002` (baseline entry no longer exists)
or `PAS0003` (surface differs from the baseline). These scripts update the files for you.

Both sides of the lean/`.Reactive` seam are tracked. `Akavache.X` publishes
`IObservable<RxVoid>` and `ISequencer`; `Akavache.X.Reactive` compiles the same source
against System.Reactive and publishes `IObservable<Unit>` and `IScheduler`. A change to
the shared source therefore shows up as a diff on both surfaces, which is exactly the
signal you want — if only one moves, the seam has drifted.

Only projects with the MSBuild property `TrackPublicApi=true` are processed. The
`tests/`, `benchmarks/`, `samples/` and `compat/` trees opt out centrally in
`src/Directory.Build.props`, so they are never touched.

### Usage

Linux / macOS:

```bash
tools/generate-publicapi.sh                 # all tracked libraries, all buildable TFMs
tools/generate-publicapi.sh Sqlite3         # only projects whose path contains 'Sqlite3'
tools/generate-publicapi.sh Akavache.Settings
```

Windows (PowerShell):

```powershell
./tools/generate-publicapi.ps1                  # all tracked libraries
./tools/generate-publicapi.ps1 -Filter Sqlite3  # path filter
```

The optional argument is a case-sensitive substring matched against each project's
path, so you can scope a run to a single library while iterating.

### What it does per TFM

1. Empties `PublicAPI/<tfm>/PublicAPI.txt`, so the analyzer reports the *entire* current
   surface as `PAS0001`. The file has to exist — with no baseline at all the analyzer
   reports `PAS0004`, which has no fix.
2. Runs `dotnet format analyzers <proj> -f <tfm> --diagnostics PAS0001 PAS0003
   --severity info`, whose fix writes the rendered surface back into the file.

### Platform notes

* Run the script on the OS that can build the frameworks you need:
  * **Linux** builds `net8.0`+, `net10.0-android`, and — via `EnableWindowsTargeting`
    — the .NET Framework and Windows-desktop TFMs. It cannot produce the Apple
    (`-ios` / `-maccatalyst` / `-macos`) baselines.
  * **Windows** builds everything, given the matching workloads. Use
    `generate-publicapi.ps1`. This repo's Apple legs are generated on the dockur
    Windows guest (`~/dockur-windows`), where the tree is shared in-guest as
    `\\host.lan\Data\rxui\Akavache`.
  * **macOS** additionally builds the Apple TFMs natively.
* A target framework whose workload or SDK is missing is reported as failed and its
  existing baseline is restored, rather than being left empty; the rest of the run
  continues. The exit code is non-zero if any TFM failed, so CI can detect an
  incomplete run.
* The scripts set `MinVerVersionOverride` (default `255.255.255-dev`) so versioning
  does not depend on git history; override it by exporting/setting the variable first.

### SDK floor

The surface is rendered by whichever Roslyn slot the host SDK loads. C# 14 extension
blocks only round-trip from the `roslyn5.3` slot (the .NET 11 SDK line) — an older SDK
silently leaves them out of the baseline rather than writing syntax it cannot read back.
This repo uses extension blocks, so generate with the .NET 11 SDK; CI installs it, so a
baseline generated on an older SDK would fail there.

### Android caveat

The Android SDK emits `__Microsoft.Android.Resource.Designer.cs` into `obj/` during the
build, so a first-ever generation run may not see `<RootNamespace>.Resource`. Re-run
generation once the designer file exists if the next build reports it.

### When to run

* After changing any public (or `protected` on a public type) API.
* After adding a new target framework to a tracked library.
* After bumping an analyzer package that changes how the public surface is rendered.

Review the resulting diff before committing — it is the human-auditable record of your
public API change.
