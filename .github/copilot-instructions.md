# Akavache: Asynchronous Key-Value Store

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- **CRITICAL**: Requires .NET 9.0 SDK (not .NET 8.0). Install with:
  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
  export PATH="$HOME/.dotnet:$PATH"
  ```
- **Platform Support**: This project now has **excellent cross-platform support** with proper setup. Windows has full support; Linux/macOS have comprehensive support for core libraries, Android, and MAUI development.
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
  dotnet restore Akavache.sln
  dotnet build Akavache.sln --configuration Release
  ```
  Build time: **15-25 minutes**. NEVER CANCEL - set timeout to 45+ minutes.

### Linux/macOS Development (Comprehensive Support)
- **CRITICAL**: Install .NET 9.0 SDK first, then install required workloads:
  ```bash
  # Install Android and MAUI workloads (recommended for cross-platform development)
  dotnet workload install android maui-android
  
  # Optional: Install additional workloads as needed
  # dotnet workload install maui  # For full MAUI support
  ```
- **What works on Linux/macOS**:
  - ✅ All core libraries (Akavache.Core, Akavache.SystemTextJson, etc.)
  - ✅ Android projects (net9.0-android) - requires Android workloads
  - ✅ MAUI applications - samples build successfully (~85 seconds)
  - ✅ Cross-platform .NET 9 projects (net9.0)
  - ✅ Standard library projects (netstandard2.0, net8.0)
  - ✅ Tests with explicit targeting: `-p:TargetFramework=net9.0`
- **What fails on Linux/macOS**:
  - ❌ Windows-specific projects (WPF samples, net9.0-windows)
  - ❌ .NET Framework projects (net462, net472)
- **Building individual projects**: Always use explicit targeting when needed:
  ```bash
  cd src
  dotnet build Akavache.Core/Akavache.csproj -p:TargetFramework=net9.0
  dotnet build Samples/AkavacheTodoMaui/AkavacheTodoMaui.csproj  # Works fully
  ```
- **Important**: **DO NOT** attempt to build Windows-specific projects or the full solution on Linux - it will fail with clear framework targeting errors.

### Testing
- **CRITICAL**: Test execution requires platform-specific configuration and .NET 9.0 SDK.
- **Windows**: Full test suite runs successfully:
  ```bash
  cd src
  dotnet test --configuration Release
  ```
  Test time: **5-15 minutes**. NEVER CANCEL - set timeout to 30+ minutes.
- **Linux/macOS**: Test execution with explicit .NET 9 targeting:
  ```bash
  cd src
  dotnet test Akavache.Tests/Akavache.Tests.csproj -p:TargetFramework=net9.0
  ```
  Test time: **3-10 minutes**. Works reliably with proper setup.

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
- **Modern C# features (C# 8–12)**:
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
  - Add default parameters to lambda expressions to reduce overloads.

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
  Time: **2-5 seconds per command**.

### Code Analysis Validation
- **Run analyzers** to check StyleCop and code quality compliance:
  ```bash
  cd src
  dotnet build --configuration Release --verbosity normal
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
  dotnet run -c Release -p Akavache.Benchmarks/Akavache.Benchmarks.csproj
  ```
  Benchmark time: **10-30 minutes**. NEVER CANCEL - set timeout to 45+ minutes.

### Compatibility Testing
- Cross-version compatibility via PowerShell script (Windows only):
  ```powershell
  .\src\RunCompatTest.ps1
  ```

## Key Projects and Structure

### Core Libraries (Priority Order)
1. **Akavache.Core** (`Akavache.csproj`) - Foundation interfaces and base implementations
2. **Akavache.Sqlite3** - SQLite-based persistent cache (most commonly used)
3. **Akavache.SystemTextJson** - Modern JSON serialization (recommended for new projects)
4. **Akavache.NewtonsoftJson** - Legacy JSON serialization (for compatibility)
5. **Akavache.EncryptedSqlite3** - Encrypted persistent cache
6. **Akavache.Settings** - Configuration and settings management
7. **Akavache.Drawing** - Image/bitmap caching support

### Sample Applications
- **AkavacheTodoWpf** - Windows WPF desktop application (Windows only)
- **AkavacheTodoMaui** - Cross-platform MAUI application (requires workloads)
- **Samples/README.md** - Comprehensive usage examples and patterns

### Testing and Benchmarks
- **Akavache.Tests** - Main test suite (168 C# files, comprehensive coverage)
- **Akavache.Settings.Tests** - Settings-specific tests
- **Akavache.Benchmarks** - V11 performance benchmarks
- **Akavache.Benchmarks.V10** - V10 comparison benchmarks

## Common Development Tasks

### Making Changes to Core Libraries
1. **Always** start with .NET 9.0 SDK installation and required workloads:
   ```bash
   # Essential first steps for any platform
   curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
   export PATH="$HOME/.dotnet:$PATH"
   dotnet workload install android maui-android  # For cross-platform development
   ```
2. **Linux/macOS**: Work with single-project targeting:
   ```bash
   dotnet build Akavache.Core/Akavache.csproj -p:TargetFramework=net9.0
   ```
3. **Always** run formatting validation:
   ```bash
   dotnet format whitespace --verify-no-changes
   ```
4. **Test on Windows** for full validation when possible.

### Adding New Features
1. **Follow coding standards** - see ReactiveUI guidelines: https://www.reactiveui.net/contribute/index.html
2. **Ensure StyleCop compliance** - all code must pass StyleCop analyzers (SA* rules)
3. **Run code analysis** - `dotnet build` must complete without analyzer warnings
4. **Add unit tests** - all features require test coverage
5. **Update documentation** - especially for public APIs with XML doc comments
6. **Run benchmarks** if performance-related changes

### Working with Samples
- **WPF Sample**: Windows only - demonstrates desktop patterns
- **MAUI Sample**: Cross-platform - demonstrates mobile/desktop patterns and **builds successfully on Linux/macOS**
- **Always** test samples when making core library changes
- **Linux/macOS tip**: MAUI samples often build faster than on Windows (~85s vs 5-10 minutes)

## Build Timing and Expectations

| Operation | Windows | Linux/macOS | Notes |
|-----------|---------|-------------|-------|
| **Single Project Restore** | 1-2 minutes | 1-2 minutes | Fast operation |
| **Single Project Build** | 2-5 minutes | 2-5 minutes | Usually works |
| **Core Library Build** | 2-15 seconds | 2-15 seconds | Very fast with .NET 9 |
| **MAUI Sample Build** | 5-10 minutes | ~85 seconds | Linux/macOS often faster |
| **WPF Sample Build** | 2-5 minutes | FAILS | Windows-only (net9.0-windows) |
| **Full Solution Restore** | 5-10 minutes | FAILS | Windows-specific deps |
| **Full Solution Build** | 15-25 minutes | FAILS | Windows-specific deps |
| **Test Suite** | 5-15 minutes | 3-10 minutes | Requires explicit .NET 9 targeting |
| **Benchmarks** | 10-30 minutes | N/A | Windows recommended |
| **Code Formatting** | 2-5 seconds | 2-5 seconds | Always works |

## Performance Characteristics

### V11 Performance (from benchmarks)
- **GetOrFetch Pattern**: 1.5ms (small) to 45ms (large datasets)
- **Bulk Operations**: 10x faster than individual operations
- **In-Memory Operations**: 2.4ms (small) to 123ms (1000 operations)
- **Cache Types**: ~27ms (small) to ~2,600ms (large datasets)

### Known Limitations
- **Windows-only projects**: WPF samples and .NET Framework targets cannot build on Linux/macOS
- **Full solution builds**: Cannot build on Linux/macOS due to Windows-specific projects
- **Large Sequential Reads**: Up to 8.6% slower than V10 in some cases
- **Package Dependencies**: More granular structure requires careful workload management

## Migration and Compatibility

### From V10 to V11
- **Breaking Changes**: Yes - new builder pattern required
- **Data Compatibility**: Full backward compatibility with cross-serializer support
- **Migration Path**: Available in documentation and samples
- **Performance Impact**: Generally equivalent or improved

### Serializer Selection
- **System.Text.Json**: Recommended for new projects (better performance)
- **Newtonsoft.Json**: For legacy compatibility or specific JSON requirements
- **BSON variants**: Available for both serializers

## CI/CD Integration

### GitHub Actions (Windows-based)
- Uses `reactiveui/actions-common` workflow
- Requires Windows runner for full build
- Installs all workloads automatically
- Runs comprehensive test suite and uploads coverage

### Local Development
- **Windows**: Use for full builds, comprehensive testing, and release validation
- **Linux/macOS**: Excellent for cross-platform development, core libraries, MAUI apps, and Android development
- **Format code** before every commit
- **Test sample applications** when changing core functionality
- **Cross-platform workflow**: Develop on Linux/macOS, validate on Windows for releases

## Troubleshooting

### Common Issues
1. **"The current .NET SDK does not support targeting .NET 9.0"**: Install .NET 9.0 SDK first
2. **"Invalid framework identifier" errors**: Use explicit `-p:TargetFramework=net9.0`
3. **"Workload not supported" errors**: Install required workloads with `dotnet workload install android maui-android`
4. **"To build a project targeting Windows on this operating system, set the EnableWindowsTargeting property to true"**: Expected on Linux/macOS for Windows-specific projects
5. **Build hangs**: Normal for large builds - wait up to 45 minutes
6. **Test failures**: May be platform-specific - verify on Windows

### Quick Fixes
- **Setup issues**: Ensure .NET 9.0 SDK is installed first, then install workloads
- **Format issues**: Run `dotnet format whitespace` and `dotnet format style`
- **StyleCop violations**: Check `.editorconfig` rules and `src/stylecop.json` configuration
- **Analyzer warnings**: Build with `--verbosity normal` to see detailed analyzer messages
- **Missing XML documentation**: All public APIs require XML doc comments per StyleCop rules
- **Package restore issues**: Clear NuGet cache with `dotnet nuget locals all --clear`
- **Build configuration errors**: Use single project builds with explicit targeting
- **Workload issues**: Install Android/MAUI workloads for cross-platform development

### When to Escalate
- **Cross-platform compatibility** issues affecting core libraries
- **Performance regressions** detected in benchmarks
- **Test failures** that persist across platforms
- **Build system changes** affecting CI/CD pipeline

## Resources

### Akavache
- **Main Repository**: https://github.com/reactiveui/Akavache
- **Repository README**: https://github.com/reactiveui/Akavache#readme
- **Issues & Bug Reports**: https://github.com/reactiveui/Akavache/issues
- **Contributing Guidelines**: https://github.com/reactiveui/Akavache/blob/main/CONTRIBUTING.md
- **Code of Conduct**: https://github.com/reactiveui/Akavache/blob/main/CODE_OF_CONDUCT.md
- **NuGet Packages**: https://www.nuget.org/packages?q=akavache
- **Main Package (Sqlite3)**: https://www.nuget.org/packages/akavache.sqlite3
- **Code Coverage**: https://codecov.io/gh/reactiveui/akavache
- **GitHub Actions (CI/CD)**: https://github.com/reactiveui/Akavache/actions
- **Sample Applications**: See `src/Samples/` directory with WPF, MAUI examples
- **Performance Reports**: `src/PERFORMANCE_SUMMARY.md` and `src/BENCHMARK_REPORT.md`
- **Community Support**: [StackOverflow - Akavache tag](https://stackoverflow.com/questions/tagged/Akavache)
- **Incubator Project**: [ReactiveMarbles.CacheDatabase](https://github.com/reactivemarbles/CacheDatabase)

### Governance & Contributing
- **Contribution Hub**: https://www.reactiveui.net/contribute/index.html
- **ReactiveUI Repository README**: https://github.com/reactiveui/ReactiveUI#readme
- **Contributing Guidelines**: https://github.com/reactiveui/ReactiveUI/blob/main/CONTRIBUTING.md
- **Code of Conduct**: https://github.com/reactiveui/ReactiveUI/blob/main/CODE_OF_CONDUCT.md

### Engineering & Style
- **ReactiveUI Coding/Style Guidance** (start here): https://www.reactiveui.net/contribute/
- **Build & Project Structure Reference**: https://github.com/reactiveui/ReactiveUI#readme

### Documentation & Samples
- **Documentation Home**: https://www.reactiveui.net/
- **Handbook** (core concepts like ReactiveObject, commands, routing): https://www.reactiveui.net/docs/
- **Official Samples Repository**: https://github.com/reactiveui/ReactiveUI.Samples

### Ecosystem
- **Splat** (service location/DI and logging): https://github.com/reactiveui/splat
- **DynamicData** (reactive collections): https://github.com/reactivemarbles/DynamicData

### Source Generators & AOT/Trimming
- **ReactiveUI.SourceGenerators**: https://github.com/reactiveui/ReactiveUI.SourceGenerators
- **.NET Native AOT Overview**: https://learn.microsoft.com/dotnet/core/deploying/native-aot/
- **Prepare Libraries for Trimming**: https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming
- **Trimming Options (MSBuild)**: https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-options
- **Fixing Trim Warnings**: https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-warnings

### Copilot Coding Agent
- **Best Practices for Copilot Coding Agent**: https://gh.io/copilot-coding-agent-tips

### CI & Misc
- **GitHub Actions** (Windows builds and workflow runs): https://github.com/reactiveui/ReactiveUI/actions
- **ReactiveUI Website Source** (useful for docs cross-refs): https://github.com/reactiveui/website
