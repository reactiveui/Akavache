# Akavache: Asynchronous Key-Value Store

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- **CRITICAL**: Requires .NET 9.0 SDK (not .NET 8.0). Install with:
  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
  export PATH="$HOME/.dotnet:$PATH"
  ```
- **Platform Support**: This project **builds fully only on Windows**. Linux/macOS have partial support.
- **Development Tools**: Visual Studio 2022 or VS Code with C# extension.

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

### Linux/macOS Development (Limited Support)
- Install available workloads:
  ```bash
  dotnet workload install android maui-android
  ```
- **IMPORTANT**: Full solution build will fail due to Windows-specific dependencies (WPF samples, Windows APIs).
- **Workaround**: Build individual projects with explicit targeting:
  ```bash
  cd src
  dotnet restore Akavache.SystemTextJson/Akavache.SystemTextJson.csproj -p:TargetFramework=net9.0
  dotnet build Akavache.SystemTextJson/Akavache.SystemTextJson.csproj -p:TargetFramework=net9.0 --no-restore
  ```
- **NEVER** attempt to build the full solution on Linux - it will fail with framework targeting errors.

### Testing
- **CRITICAL**: Test execution requires platform-specific configuration.
- Windows: Full test suite runs successfully:
  ```bash
  cd src
  dotnet test --configuration Release
  ```
  Test time: **5-15 minutes**. NEVER CANCEL - set timeout to 30+ minutes.
- Linux: Limited test execution with explicit targeting:
  ```bash
  cd src
  dotnet test Akavache.Tests/Akavache.Tests.csproj -p:TargetFramework=net9.0 --list-tests
  ```

## Validation and Quality Assurance

### Code Style and Analysis Enforcement
- **EditorConfig Compliance**: Repository uses comprehensive `.editorconfig` with 500+ rules for C# formatting, naming conventions, and code analysis
- **StyleCop Analyzers**: Enforces consistent C# code style with `stylecop.analyzers` (v1.2.0-beta.556)
- **Roslynator Analyzers**: Additional code quality rules with `Roslynator.Analyzers` (v4.14.0)
- **Analysis Level**: Set to `latest` with enhanced .NET analyzers enabled
- **CRITICAL**: All code must comply with **ReactiveUI contribution guidelines**: https://www.reactiveui.net/contribute/index.html

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
  - Analyzer packages in `src/Directory.build.props`

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
1. **Always** work with single-project targeting on Linux:
   ```bash
   dotnet build Akavache.Core/Akavache.csproj -p:TargetFramework=net9.0
   ```
2. **Always** run formatting validation:
   ```bash
   dotnet format whitespace --verify-no-changes
   ```
3. **Test on Windows** for full validation when possible.

### Adding New Features
1. **Follow coding standards** - see ReactiveUI guidelines: https://www.reactiveui.net/contribute/index.html
2. **Ensure StyleCop compliance** - all code must pass StyleCop analyzers (SA* rules)
3. **Run code analysis** - `dotnet build` must complete without analyzer warnings
4. **Add unit tests** - all features require test coverage
5. **Update documentation** - especially for public APIs with XML doc comments
6. **Run benchmarks** if performance-related changes

### Working with Samples
- **WPF Sample**: Windows only - demonstrates desktop patterns
- **MAUI Sample**: Cross-platform - demonstrates mobile/desktop patterns
- **Always** test samples when making core library changes

## Build Timing and Expectations

| Operation | Windows | Linux/macOS | Notes |
|-----------|---------|-------------|-------|
| **Single Project Restore** | 1-2 minutes | 1-2 minutes | Fast operation |
| **Single Project Build** | 2-5 minutes | 2-5 minutes | Usually works |
| **Full Solution Restore** | 5-10 minutes | FAILS | Windows-specific deps |
| **Full Solution Build** | 15-25 minutes | FAILS | Windows-specific deps |
| **Test Suite** | 5-15 minutes | LIMITED | Platform limitations |
| **Benchmarks** | 10-30 minutes | N/A | Windows recommended |
| **Code Formatting** | 2-5 seconds | 2-5 seconds | Always works |

## Performance Characteristics

### V11 Performance (from benchmarks)
- **GetOrFetch Pattern**: 1.5ms (small) to 45ms (large datasets)
- **Bulk Operations**: 10x faster than individual operations
- **In-Memory Operations**: 2.4ms (small) to 123ms (1000 operations)
- **Cache Types**: ~27ms (small) to ~2,600ms (large datasets)

### Known Limitations
- **Linux Build**: Cannot build Windows-specific projects (WPF, Windows APIs)
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
- **Always** use Windows for release builds and comprehensive testing
- **Use** Linux/macOS for quick iteration on core libraries only
- **Format code** before every commit
- **Test sample applications** when changing core functionality

## Troubleshooting

### Common Issues
1. **"Invalid framework identifier" errors**: Use explicit `-p:TargetFramework=net9.0`
2. **"Workload not supported" errors**: Platform limitation - use Windows
3. **Build hangs**: Normal for large builds - wait up to 45 minutes
4. **Test failures**: May be platform-specific - verify on Windows

### Quick Fixes
- **Format issues**: Run `dotnet format whitespace` and `dotnet format style`
- **StyleCop violations**: Check `.editorconfig` rules and `src/stylecop.json` configuration
- **Analyzer warnings**: Build with `--verbosity normal` to see detailed analyzer messages
- **Missing XML documentation**: All public APIs require XML doc comments per StyleCop rules
- **Package restore issues**: Clear NuGet cache with `dotnet nuget locals all --clear`
- **Build configuration errors**: Use single project builds with explicit targeting

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