# CLAUDE.md

## Repository Orientation

- **Primary working directory for build/test:** `./src`
- **Main solution:** `src/Akavache.slnx`
- **Production libraries:**
  - `src/Akavache.Core/` — Foundation interfaces (`IBlobCache`, `CacheDatabase`, `InMemoryBlobCacheBase`), helpers, serializer abstractions
  - `src/Akavache.Sqlite3/` — SQLite-backed cache using direct SQLitePCLRaw interop (`SqlitePclRawConnection`, `SqliteOperationQueue`, `SqliteBlobCache`)
  - `src/Akavache.EncryptedSqlite3/` — Encrypted SQLite cache (compiles Sqlite3 sources with `ENCRYPTED` define, uses SQLite3MC for encryption)
  - `src/Akavache.SystemTextJson/` — System.Text.Json serializer
  - `src/Akavache.SystemTextJson.Bson/` — System.Text.Json BSON serializer
  - `src/Akavache.NewtonsoftJson/` — Newtonsoft.Json serializer
  - `src/Akavache.HttpDownloader/` — HTTP download extensions (`HttpService`, `HttpExtensions`, `RelativeTimeDownloadExtensions`)
  - `src/Akavache.Drawing/` — Image/bitmap caching support
  - `src/Akavache.Settings/` — Typed settings storage built on top of blob caches
  - `src/Akavache.V10toV11/` — V10-to-V11 data migration utilities
- **Test projects (11 assemblies):**
  - Serial assemblies (use `[assembly: NotInParallel]` + custom `TestExecutor`):
    - `src/tests/Akavache.Core.Tests/` — Tests touching global state (`CacheDatabase`, `AppLocator`, `UniversalSerializer`)
    - `src/tests/Akavache.Sqlite3.Tests/` — SQLite builder extensions, backward compatibility, legacy file locations
    - `src/tests/Akavache.EncryptedSqlite3.Tests/` — Encrypted builder extensions
    - `src/tests/Akavache.Integration.Tests/` — Builder interop, AOT compatibility, universal serializer, Drawing/Image tests
    - `src/tests/Akavache.Settings.Tests/` — Settings builder, ambient cache, fallback tests
  - Parallel assemblies (no executor, TUnit default parallel):
    - `src/tests/Akavache.Core.Tests.Parallel/` — Pure unit tests (CacheEntry, helpers, observables, concurrency)
    - `src/tests/Akavache.Sqlite3.Tests.Parallel/` — SqliteBlobCache CRUD, bulk ops, operation queue, reply/row observables
    - `src/tests/Akavache.EncryptedSqlite3.Tests.Parallel/` — Encrypted blob cache CRUD, bulk ops, direct tests
    - `src/tests/Akavache.Integration.Tests.Parallel/` — Serializer tests (STJ, Newtonsoft, BSON), size tests, error handling
    - `src/tests/Akavache.Settings.Tests.Parallel/` — Settings cache, encrypted settings, value subjects, storage
  - HTTP-isolated assembly:
    - `src/tests/Akavache.HttpDownloader.Tests/` — HTTP download/extension tests (isolated to avoid TCP socket contention)
  - `src/tests/shared/` — Shared test infrastructure (Helpers, Mocks, TestBases) compiled into each assembly via `<Compile Include>`
- **Sample apps:** `src/samples/`
- **Benchmarks:** `src/benchmarks/`
- **Compatibility writers/readers:** `src/compat/`

## Build

```bash
# Restore & build (from src/)
dotnet build Akavache.slnx

# Build specific project
dotnet build Akavache.Core/Akavache.csproj
```

## Testing: Microsoft Testing Platform (MTP) + TUnit

This repo uses **Microsoft Testing Platform (MTP)** with **TUnit** (not VSTest).

- MTP is configured via `src/global.json`
- Additional test settings in `src/testconfig.json`
- `TestingPlatformDotnetTestSupport` is enabled in `src/Directory.Build.props`
- `IsTestProject` is auto-detected via `$(MSBuildProjectName.Contains('Tests'))` in `Directory.Build.props`
- Serial assemblies enforce `[assembly: NotInParallel]` with a custom `AkavacheTestExecutor` that resets global state between tests
- Parallel assemblies have no executor and run tests concurrently (TUnit default)

**Key rule:** TUnit/MTP arguments go **after** `--`.

### Test Commands (run from `./src`)

**CRITICAL:** Run `dotnet test` from the `src/` directory so the `global.json` MTP runner config is discovered. Use `--project` to specify the test project.

```bash
cd src

# Run a specific test assembly
dotnet test --project tests/Akavache.Core.Tests/Akavache.Core.Tests.csproj

# Detailed output (place BEFORE --)
dotnet test --project tests/Akavache.Core.Tests/Akavache.Core.Tests.csproj -- --output Detailed

# List tests
dotnet test --project tests/Akavache.Core.Tests/Akavache.Core.Tests.csproj -- --list-tests

# Fail fast
dotnet test --project tests/Akavache.Core.Tests/Akavache.Core.Tests.csproj -- --fail-fast

# Run specific test by filter
dotnet test --project tests/Akavache.Core.Tests/Akavache.Core.Tests.csproj -- --treenode-filter "/*/*/*/MyTestMethod"

# All tests in a class
dotnet test --project tests/Akavache.Core.Tests/Akavache.Core.Tests.csproj -- --treenode-filter "/*/*/MyClassName/*"
```

### Testing Best Practices

- **Do NOT use `--no-build`** — always build before testing to avoid stale binaries.
- Use `--output Detailed` **before** `--` for verbose output.

### Code Coverage

Code coverage uses **Microsoft.Testing.Extensions.CodeCoverage** configured in `src/testconfig.json`. Coverage is collected for production assemblies only (test projects and TestRunner are excluded).

```bash
# Run tests with code coverage (from src/ folder)
dotnet test --project tests/Akavache.Integration.Tests/Akavache.Integration.Tests.csproj -- --coverage --coverage-output-format cobertura
dotnet test --project tests/Akavache.Settings.Tests/Akavache.Settings.Tests.csproj -- --coverage --coverage-output-format cobertura

# Generate HTML report using ReportGenerator (install if needed: dotnet tool install -g dotnet-reportgenerator-globaltool)
# Find all cobertura files and generate report to /tmp/<folder>
# Linux/macOS
reportgenerator \
  -reports:"**/TestResults/**/*.cobertura.xml" \
  -targetdir:/tmp/akavache_coverage \
  -reporttypes:"Html;TextSummary"
cat /tmp/akavache_coverage/Summary.txt
xdg-open /tmp/akavache_coverage/index.html   # Linux
open /tmp/akavache_coverage/index.html        # macOS

# Windows (PowerShell)
reportgenerator `
  -reports:"**/TestResults/**/*.cobertura.xml" `
  -targetdir:"$env:TEMP\akavache_coverage" `
  -reporttypes:"Html;TextSummary"
Get-Content "$env:TEMP\akavache_coverage\Summary.txt"
Start-Process "$env:TEMP\akavache_coverage\index.html"
```

**Key configuration** (`src/testconfig.json`):
- `modulePaths.include`: `Akavache\\..*` — covers all production assemblies
- `modulePaths.exclude`: `.*Tests.*`, `.*TestRunner.*` — excludes test/runner assemblies
- `skipAutoProperties: true` — auto-properties excluded from coverage metrics

**Using the MCP coverage tool** (if `dotnet-mtp-coverage-mcp` MCP server is available):

The `dotnet-mtp-coverage-mcp` MCP server provides tools to query Cobertura XML coverage reports directly without needing ReportGenerator:

- `get_solution_coverage` — overall solution-level coverage summary
- `get_project_coverage` — coverage for a specific project/package (e.g., `"Akavache.Settings"`)
- `get_missed_coverage_for_file` — shows missed lines and branches for a specific source file
- `get_class_coverage` — coverage for a specific class
- `get_method_coverage` — coverage for a specific method

All MCP tools require the `coberturaFilePath` parameter pointing to the generated `.cobertura.xml` file. Find it after a coverage run:

```bash
find src -name "*.cobertura.xml" -path "*/TestResults/*"
```

**Workflow for improving coverage:**

1. Run tests with `--coverage --coverage-output-format cobertura`
2. Find the generated `.cobertura.xml` file
3. Use `get_missed_coverage_for_file` to see exactly which lines/branches are uncovered
4. Write tests targeting the uncovered code paths
5. Re-run coverage and verify improvement

**Tips:**
- Always clean `bin/` and `obj/` folders before coverage runs to avoid stale results
- Put coverage reports in a temp directory (`/tmp/` on Linux/macOS, `$env:TEMP` on Windows) to avoid accidentally committing them

## Code Style

- Follow ReactiveUI contribution guidelines
- `.editorconfig` formatting/naming conventions enforced
- StyleCop and Roslynator analyzers active
- Public APIs require XML documentation
- No `#pragma warning disable` in production code — use `[SuppressMessage]` attribute instead
- All production methods must be `internal` (not `private`) so they can be tested
- No default parameter values on public interfaces/methods (binary-break hazard) — use explicit overloads
