# Akavache: Asynchronous Key-Value Store

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- **CRITICAL**: Requires .NET 10, 9, and 8 SDKs. Always install all versions fresh (don't check what's installed):
  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 10.0
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 8.0
  export PATH="$HOME/.dotnet:$PATH"
  ```
- **Platform Support**: This project has cross-platform support with proper setup. Windows has full support; Linux/macOS have comprehensive support for core libraries, Android, and MAUI development.
- **Development Tools**: Visual Studio 2022 or VS Code with C# extension.
- Note on Cloning the Repository
  When cloning the Akavache repository, use a full clone instead of a shallow one (e.g., avoid --depth=1). This project uses Nerdbank.GitVersioning for automatic version calculation based on Git history. Shallow clones lack the necessary commit history, which can cause build errors or force the tool to perform an extra fetch step to deepen the repository. To ensure smooth builds:
   ```bash
   git clone https://github.com/reactiveui/Akavache.git
   ```
   If you've already done a shallow clone, deepen it with:
   ```bash
   git fetch --unshallow
   ```
   This prevents exceptions like "Shallow clone lacks the objects required to calculate version height."

### Windows Development (Full Support)
- Install .NET workloads for cross-platform development:
  ```bash
  dotnet workload install android ios tvos macos maui maccatalyst
  ```
- Full solution restore and build:
  ```bash
  cd src
  dotnet build Akavache.slnx
  ```

### Linux/macOS Development (Comprehensive Support)
- **CRITICAL**: Install .NET 10, 9, and 8 SDKs first, then install required workloads:
  ```bash
  # Install .NET SDKs (all versions fresh)
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 10.0
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 8.0
  export PATH="$HOME/.dotnet:$PATH"

  # Install Android and MAUI workloads (recommended for cross-platform development)
  dotnet workload install android maui-android
  ```
- **What works on Linux/macOS**:
  - All core libraries (Akavache.Core, Akavache.SystemTextJson, etc.)
  - Android projects (net9.0-android, net10.0-android) - requires Android workloads
  - MAUI applications - samples build successfully
  - All test assemblies (`dotnet test` from src/)
- **What fails on Linux/macOS**:
  - Windows-specific projects (WPF samples, net9.0-windows)
  - .NET Framework projects (net462, net472, net481)
- **Building individual projects**: Always use explicit targeting when needed:
  ```bash
  cd src
  dotnet build Akavache.Core/Akavache.csproj -p:TargetFramework=net10.0
  ```

### Testing
- **CRITICAL**: Test execution requires .NET SDKs (10, 9, and 8).
- The solution uses **slnx** format: `Akavache.slnx`
- Test commands — **always run from `src/` directory**:
  ```bash
  cd src

  # Run a specific test assembly
  dotnet test --project tests/Akavache.Core.Tests/Akavache.Core.Tests.csproj
  dotnet test --project tests/Akavache.Sqlite3.Tests/Akavache.Sqlite3.Tests.csproj
  dotnet test --project tests/Akavache.EncryptedSqlite3.Tests/Akavache.EncryptedSqlite3.Tests.csproj
  dotnet test --project tests/Akavache.Integration.Tests/Akavache.Integration.Tests.csproj
  dotnet test --project tests/Akavache.Settings.Tests/Akavache.Settings.Tests.csproj
  dotnet test --project tests/Akavache.Http.Tests/Akavache.Http.Tests.csproj

  # Parallel assemblies
  dotnet test --project tests/Akavache.Core.Tests.Parallel/Akavache.Core.Tests.Parallel.csproj
  dotnet test --project tests/Akavache.Sqlite3.Tests.Parallel/Akavache.Sqlite3.Tests.Parallel.csproj
  dotnet test --project tests/Akavache.EncryptedSqlite3.Tests.Parallel/Akavache.EncryptedSqlite3.Tests.Parallel.csproj
  dotnet test --project tests/Akavache.Integration.Tests.Parallel/Akavache.Integration.Tests.Parallel.csproj
  dotnet test --project tests/Akavache.Settings.Tests.Parallel/Akavache.Settings.Tests.Parallel.csproj
  ```

### Test Architecture

Tests are split into **serial** and **parallel** assemblies:

- **Serial assemblies** (`*.Tests`): Use `[assembly: NotInParallel]` with a custom `AkavacheTestExecutor` that resets global state (`CacheDatabase`, `AppLocator`, `UniversalSerializer`) between tests. Tests that touch shared singletons live here.
- **Parallel assemblies** (`*.Tests.Parallel`): No executor, TUnit default parallel execution. Tests that create isolated cache instances and don't touch global state live here.
- **HTTP-isolated assembly** (`Akavache.Http.Tests`): Dedicated assembly for HTTP download tests to avoid TCP socket contention when MTP runs assemblies simultaneously.
- **Shared infrastructure** (`tests/shared/`): Helpers, Mocks, and TestBases compiled into each assembly via `<Compile Include>` in csproj files.
- `IsTestProject` is auto-detected via `$(MSBuildProjectName.Contains('Tests'))` in `Directory.Build.props`.

### Test Practices
- **Do NOT use `--no-build`** — always build before testing.
- Use `ImmediateScheduler.Instance` for `InMemoryBlobCache` in tests to ensure synchronous observable completion.
- Use `WaitForValue()` / `WaitForCompletion()` / `WaitForError()` for real SQLite caches (async delivery).
- Use `SubscribeGetValue()` / `SubscribeAndComplete()` / `SubscribeGetError()` only for synchronous observables.
- Never use `.Timeout()` with `Subscribe()` or `SubscribeGetValue()` — unobserved timeout errors crash the process.
- TUnit/MTP arguments go **after** `--` (e.g., `-- --output Detailed`).

## Validation and Quality Assurance

### Code Style and Analysis Enforcement
- **EditorConfig Compliance**: Repository uses a comprehensive `.editorconfig` with detailed rules for C# formatting, naming conventions, and code analysis.
- **StyleCop Analyzers**: Enforces consistent C# code style with `stylecop.analyzers`.
- **Roslynator Analyzers**: Additional code quality rules with `Roslynator.Analyzers`.
- **Analysis Level**: Set to `latest` with enhanced .NET analyzers enabled.
- **CRITICAL**: All code must comply with ReactiveUI contribution guidelines: [https://www.reactiveui.net/contribute/index.html](https://www.reactiveui.net/contribute/index.html).

## C# Style Guide
**General Rule**: Follow "Visual Studio defaults" with the following specific requirements:

### Brace Style
- **Allman style braces**: Each brace begins on a new line.
- **Single line statement blocks**: Can go without braces but must be properly indented on its own line and not nested in other statement blocks that use braces.
- **Exception**: A `using` statement is permitted to be nested within another `using` statement by starting on the following line at the same indentation level, even if the nested `using` contains a controlled block.

### Indentation and Spacing
- **Indentation**: Four spaces (no tabs).
- **Spurious free spaces**: Avoid, e.g., `if (someVar == 0)...` where dots mark spurious spaces.
- **Empty lines**: Avoid more than one empty line at any time between members of a type.
- **Labels**: Indent one less than the current indentation (for `goto` statements).

### Field and Property Naming
- **Internal and private fields**: Use `_camelCase` prefix with `readonly` where possible.
- **Static fields**: `readonly` should come after `static` (e.g., `static readonly` not `readonly static`).
- **Public fields**: Use PascalCasing with no prefix (use sparingly).
- **Constants**: Use PascalCasing for all constant local variables and fields (except interop code, where names and values must match the interop code exactly).
- **Fields placement**: Specify fields at the top within type declarations.

### Visibility and Modifiers
- **Always specify visibility**: Even if it's the default (e.g., `private string _foo` not `string _foo`).
- **Visibility first**: Should be the first modifier (e.g., `public abstract` not `abstract public`).
- **Modifier order**: `public`, `private`, `protected`, `internal`, `static`, `extern`, `new`, `virtual`, `abstract`, `sealed`, `override`, `readonly`, `unsafe`, `volatile`, `async`.
- **All production methods must be `internal`** (not `private`) so every method can have matching tests.

### Namespace and Using Statements
- **Namespace imports**: At the top of the file, outside of `namespace` declarations.
- **Sorting**: System namespaces alphabetically first, then third-party namespaces alphabetically.
- **Global using directives**: Use where appropriate to reduce repetition across files.
- **Placement**: Use `using` directives outside `namespace` declarations.

### Type Usage and Variables
- **Language keywords**: Use instead of BCL types (e.g., `int`, `string`, `float` instead of `Int32`, `String`, `Single`) for type references and method calls (e.g., `int.Parse` instead of `Int32.Parse`).
- **var usage**: Encouraged for large return types or refactoring scenarios; use full type names for clarity when needed.
- **this. avoidance**: Avoid `this.` unless absolutely necessary.
- **nameof(...)**: Use instead of string literals whenever possible and relevant.

### Code Patterns and Features
- **Method groups**: Use where appropriate.
- **Pattern matching**: Use C# 7+ pattern matching, including recursive, tuple, positional, type, relational, and list patterns for expressive conditional logic.
- **Inline out variables**: Use C# 7 inline variable feature with `out` parameters.
- **Non-ASCII characters**: Use Unicode escape sequences (`\uXXXX`) instead of literal characters to avoid garbling by tools or editors.
- **Modern C# features (C# 8–14)**:
  - Enable nullable reference types to reduce null-related errors.
  - Use ranges (`..`) and indices (`^`) for concise collection slicing.
  - Employ `using` declarations for automatic resource disposal.
  - Declare static local functions to avoid state capture.
  - Prefer switch expressions over statements for concise control flow.
  - Use records and record structs for data-centric types with value semantics.
  - Apply init-only setters for immutable properties.
  - Utilize target-typed `new` expressions to reduce verbosity.
  - Declare static anonymous functions or lambdas to prevent state capture.
  - Use file-scoped namespace declarations for concise syntax.
  - Apply `with` expressions for nondestructive mutation.
  - Use raw string literals (`"""`) for multi-line or complex strings.
  - Mark required members with the `required` modifier.
  - Use primary constructors to centralize initialization logic.
  - Employ collection expressions (`[...]`) for concise array/list/span initialization.
  - Use C# 14 `field` keyword for auto-property backing fields where applicable.
- **No default parameters on public interfaces/methods**: Use explicit overloads instead (binary-break hazard).
- **No `#pragma warning disable` in production code**: Use `[SuppressMessage]` attribute instead.

### Documentation Requirements
- **XML comments**: All publicly exposed methods and properties must have .NET XML comments, including protected methods of public classes.
- **Documentation culture**: Use `en-US` as specified in `src/stylecop.json`.

### File Style Precedence
- **Existing style**: If a file differs from these guidelines (e.g., private members named `m_member` instead of `_member`), the existing style in that file takes precedence.
- **Consistency**: Maintain consistency within individual files.

### Code Formatting (Fast - Always Run)
- **ALWAYS** run formatting before committing:
  ```bash
  cd src
  dotnet format whitespace --verify-no-changes
  dotnet format style --verify-no-changes
  ```

### Code Analysis Validation
- **Run analyzers** to check StyleCop and code quality compliance:
  ```bash
  cd src
  dotnet build Akavache.slnx
  ```
  This runs all analyzers (StyleCop SA*, Roslynator RCS*, .NET CA*) and treats warnings as errors.
- **Analyzer Configuration**:
  - StyleCop settings in `src/stylecop.json`
  - EditorConfig rules in `.editorconfig` (root level)
  - Analyzer packages in `src/Directory.Build.props`

### Benchmarking
- Performance testing available via BenchmarkDotNet:
  ```bash
  cd src
  dotnet run -c Release -p benchmarks/Akavache.Benchmarks/Akavache.Benchmarks.csproj
  ```

### Compatibility Testing
- Cross-version compatibility via PowerShell script (Windows only):
  ```powershell
  .\src\RunCompatTest.ps1
  ```

## Key Projects and Structure

### Core Libraries (Priority Order)
1. **Akavache.Core** (`Akavache.csproj`) - Foundation interfaces and base implementations (`IBlobCache`, `CacheDatabase`, `InMemoryBlobCacheBase`)
2. **Akavache.Sqlite3** - SQLite-based persistent cache using direct SQLitePCLRaw interop (`SqlitePclRawConnection`, `SqliteOperationQueue`, `SqliteBlobCache`)
3. **Akavache.EncryptedSqlite3** - Encrypted persistent cache (shared sources from Sqlite3 with `ENCRYPTED` define, uses SQLite3MC for encryption)
4. **Akavache.SystemTextJson** - Modern JSON serialization (recommended for new projects)
5. **Akavache.SystemTextJson.Bson** - System.Text.Json BSON serializer
6. **Akavache.NewtonsoftJson** - Legacy JSON serialization (for compatibility)
7. **Akavache.Http** - HTTP download extensions (`HttpService`, `HttpExtensions`, `RelativeTimeDownloadExtensions`)
8. **Akavache.Settings** - Configuration and settings management (typed settings storage on blob caches)
9. **Akavache.Drawing** - Image/bitmap caching support
10. **Akavache.V10toV11** - V10-to-V11 data migration utilities

### Sample Applications
- **AkavacheTodoWpf** - Windows WPF desktop application (Windows only)
- **AkavacheTodoMaui** - Cross-platform MAUI application (requires workloads)

### Testing and Benchmarks
- **11 test assemblies**: 5 serial + 5 parallel + 1 HTTP-isolated (see Test Architecture above)
- **tests/shared/** - Shared test infrastructure (Helpers, Mocks, TestBases) compiled into each test assembly via `<Compile Include>`
- **Akavache.Benchmarks** - V12 performance benchmarks
- **Akavache.Benchmarks.V10** - V10 comparison benchmarks

## Common Development Tasks

### Making Changes to Core Libraries
1. **Always** start with .NET SDK installation (10, 9, and 8) and required workloads
2. **Build the full solution**: `dotnet build Akavache.slnx` from `src/`
3. **Run formatting validation**: `dotnet format whitespace --verify-no-changes`
4. **Run affected tests**: use `--project` to target specific test assemblies

### Adding New Features
1. **Follow coding standards** - see ReactiveUI guidelines: https://www.reactiveui.net/contribute/index.html
2. **Ensure StyleCop compliance** - all code must pass StyleCop analyzers (SA* rules)
3. **Run code analysis** - `dotnet build` must complete without analyzer warnings
4. **Add unit tests** - all features require test coverage
5. **Update documentation** - especially for public APIs with XML doc comments

## Migration and Compatibility

### From V10 to V11
- **Breaking Changes**: Yes - new builder pattern required
- **Data Compatibility**: Full backward compatibility with cross-serializer support
- **Migration Path**: Available via `Akavache.V10toV11` package

### Serializer Selection
- **System.Text.Json**: Recommended for new projects (better performance, AOT-compatible)
- **Newtonsoft.Json**: For legacy compatibility or specific JSON requirements
- **BSON variants**: Available for both serializers

## CI/CD Integration

### GitHub Actions
- Uses `reactiveui/actions-common` workflow
- Runs on Windows, Linux, and macOS
- Installs all workloads automatically
- Runs comprehensive test suite across all 11 assemblies

## Resources

### Akavache
- **Main Repository**: https://github.com/reactiveui/Akavache
- **Issues & Bug Reports**: https://github.com/reactiveui/Akavache/issues
- **NuGet Packages**: https://www.nuget.org/packages?q=akavache
- **Code Coverage**: https://codecov.io/gh/reactiveui/akavache
- **GitHub Actions (CI/CD)**: https://github.com/reactiveui/Akavache/actions

### Governance & Contributing
- **Contribution Hub**: https://www.reactiveui.net/contribute/index.html
- **ReactiveUI Repository**: https://github.com/reactiveui/ReactiveUI

### Ecosystem
- **Splat** (service location/DI and logging): https://github.com/reactiveui/splat
- **DynamicData** (reactive collections): https://github.com/reactivemarbles/DynamicData

### Copilot Coding Agent
- **Best Practices for Copilot Coding Agent**: https://gh.io/copilot-coding-agent-tips
