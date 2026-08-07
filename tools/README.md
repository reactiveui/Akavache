# tools

Maintenance scripts for the Akavache repository.

## generate-publicapi

Regenerates the **PublicAPI baseline files** consumed by
`Microsoft.CodeAnalysis.PublicApiAnalyzers` (`RS0016`, `RS0017`, `RS0037`).

Each shipped library tracks its public surface per target framework in:

```
src/<Project>/PublicAPI/<tfm>/PublicAPI.Shipped.txt
src/<Project>/PublicAPI/<tfm>/PublicAPI.Unshipped.txt
```

When you add, remove, or change public API, those files must be updated or the build
fails with `RS0016` (symbol not in baseline) / `RS0017` (baseline entry not found) /
`RS0037` (missing `#nullable enable`). These scripts do that for you.

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

1. Resets both `PublicAPI/<tfm>/PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`
   to just `#nullable enable`, so the analyzer reports the *entire* current surface.
2. Runs `dotnet format analyzers <proj> -f <tfm> --diagnostics RS0016 RS0017 RS0037
   --severity info`, which fills `PublicAPI.Unshipped.txt` with the current public API.
3. Folds that surface into `PublicAPI.Shipped.txt` (ordinally sorted, deduped) and
   resets `PublicAPI.Unshipped.txt` back to the bare header. This repo keeps the full
   surface in **Shipped** with **Unshipped empty**, so a later API change shows up as
   new Unshipped lines.

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
  existing baseline is left untouched, rather than being wiped; the rest of the run
  continues. The exit code is non-zero if any TFM failed, so CI can detect an
  incomplete run.
* The scripts set `MinVerVersionOverride` (default `255.255.255-dev`) so versioning
  does not depend on git history; override it by exporting/setting the variable first.

### Android caveat

The Android SDK emits `__Microsoft.Android.Resource.Designer.cs` into `obj/` during the
build, so a first-ever generation run does not see `<RootNamespace>.Resource` and the
next build fails `RS0016` on it. Add the two lines by hand to the `net10.0-android`
baseline (as the other libraries in this org do) or re-run generation once the designer
file exists:

```
<RootNamespace>.Resource
<RootNamespace>.Resource.Resource() -> void
```

### When to run

* After changing any public (or `protected` on a public type) API.
* After adding a new target framework to a tracked library.
* After bumping an analyzer package that changes how the public surface is rendered.

Review the resulting diff before committing — it is the human-auditable record of your
public API change.
