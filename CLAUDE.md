# CLAUDE.md

## Repository Orientation

- **Primary working directory for build/test:** `./src`
- **Main solution:** `src/Akavache.slnx`
- **Test project:** `src/Akavache.Tests/Akavache.Tests.csproj`

## Build

```bash
# Restore & build
dotnet build src/Akavache.slnx

# Build specific project
dotnet build src/Akavache.Core/Akavache.csproj
```

## Testing: Microsoft Testing Platform (MTP) + TUnit

This repo uses **Microsoft Testing Platform (MTP)** with **TUnit** (not VSTest).

- MTP is configured via `src/global.json`
- Additional test settings in `src/testconfig.json`
- `TestingPlatformDotnetTestSupport` is enabled in `src/Directory.Build.props`
- Tests run **non-parallel** (`"parallel": false` in `testconfig.json`)

**Key rule:** TUnit/MTP arguments go **after** `--`.

### Test Commands (run from `./src`)

**CRITICAL:** Run `dotnet test` from the `src/` directory so the `global.json` MTP runner config is discovered. Use `--project` to specify the test project.

```bash
cd src

# Run all tests
dotnet test --project Akavache.Tests/Akavache.Tests.csproj

# Detailed output (place BEFORE --)
dotnet test --project Akavache.Tests/Akavache.Tests.csproj -- --output Detailed

# List tests
dotnet test --project Akavache.Tests/Akavache.Tests.csproj -- --list-tests

# Fail fast
dotnet test --project Akavache.Tests/Akavache.Tests.csproj -- --fail-fast

# Run specific test by filter
dotnet test --project Akavache.Tests/Akavache.Tests.csproj -- --treenode-filter "/*/*/*/MyTestMethod"

# All tests in a class
dotnet test --project Akavache.Tests/Akavache.Tests.csproj -- --treenode-filter "/*/*/MyClassName/*"
```

### Testing Best Practices

- **Do NOT use `--no-build`** — always build before testing to avoid stale binaries.
- Use `--output Detailed` **before** `--` for verbose output.

## Code Style

- Follow ReactiveUI contribution guidelines
- `.editorconfig` formatting/naming conventions enforced
- StyleCop and Roslynator analyzers active
- Public APIs require XML documentation
- No `#pragma warning disable` in production code
