# NUnit to TUnit Migration Guide for Akavache.Tests

## Migration Status

### ? Completed
- **Akavache.Settings.Tests**: Fully migrated, all 18 tests pass
- **Directory.Build.props**: Updated with TUnit packages
- **Directory.Packages.props**: NUnit versions removed

### ?? Partially Completed - Akavache.Tests
- Package references updated
- Test attributes partially converted
- AssemblyInfo.Parallel.cs configured for TUnit
- Usings.cs created with TUnit global usings

## Required Changes for Akavache.Tests

### 1. Test Attributes
Replace NUnit attributes with TUnit equivalents:

| NUnit | TUnit |
|-------|-------|
| `[TestFixture]` | Remove (not needed) |
| `[Test]` | `[Test]` (same) |
| `[TestCase(...)]` | `[Arguments(...)]` |
| `[SetUp]` | `[Before(Test)]` |
| `[TearDown]` | `[After(Test)]` |
| `[OneTimeSetUp]` | `[Before(Class)]` (must be static) |
| `[OneTimeTearDown]` | `[After(Class)]` (must be static) |
| `[NonParallelizable]` | `[NotInParallel]` |
| `[Parallelizable(...)]` | `[NotInParallel]` or remove |
| `[Ignore("reason")]` | `[Skip("reason")]` |
| `[Category("name")]` | `[Category("name")]` (same) |
| `[Combinatorial]` + `[ValueSource]` | `[MethodDataSource(nameof(Method))]` |

### 2. Assertion Patterns
Convert NUnit assertions to TUnit's async assertion style:

| NUnit | TUnit |
|-------|-------|
| `Assert.That(x, Is.EqualTo(y))` | `await Assert.That(x).IsEqualTo(y)` |
| `Assert.That(x, Is.Not.Null)` | `await Assert.That(x).IsNotNull()` |
| `Assert.That(x, Is.Null)` | `await Assert.That(x).IsNull()` |
| `Assert.That(x, Is.True)` | `await Assert.That(x).IsTrue()` |
| `Assert.That(x, Is.False)` | `await Assert.That(x).IsFalse()` |
| `Assert.That(x, Is.Empty)` | `await Assert.That(x).IsEmpty()` |
| `Assert.That(x, Is.Not.Empty)` | `await Assert.That(x).IsNotEmpty()` |
| `Assert.That(x, Is.GreaterThan(y))` | `await Assert.That(x).IsGreaterThan(y)` |
| `Assert.That(x, Is.LessThan(y))` | `await Assert.That(x).IsLessThan(y)` |
| `Assert.That(x, Has.Count.EqualTo(n))` | `await Assert.That(x).HasCount().EqualTo(n)` |
| `Assert.That(x, Does.Contain(y))` | `await Assert.That(x).Contains(y)` |
| `Assert.That(x, Is.SameAs(y))` | `await Assert.That(x).IsSameReference(y)` |
| `Assert.That(x, Is.TypeOf<T>())` | `await Assert.That(x).IsTypeOf<T>()` |

### 3. Exception Testing
| NUnit | TUnit |
|-------|-------|
| `Assert.Throws<T>(() => ...)` | `await Assert.That(() => ...).Throws<T>()` |
| `Assert.ThrowsAsync<T>(async () => ...)` | `await Assert.That(async () => ...).ThrowsException().OfType<T>()` |
| `Assert.DoesNotThrow(() => ...)` | Remove wrapper, just run the code |

### 4. Method Signatures
- All test methods with `await Assert.That(...)` must be `async Task`
- `[Before(Class)]` and `[After(Class)]` methods must be `static`
- If instance state is needed, use `[Before(Test)]` and `[After(Test)]` instead

### 5. Remove NUnit-specific constructs
- Remove `using NUnit.Framework;`
- Remove `Assert.EnterMultipleScope()` blocks
- Remove `Assert.Pass()` calls

## Files with Complex Changes Required
The following files have complex NUnit patterns that need manual review:
- AndroidInitializationTests.cs - Assert.DoesNotThrow patterns
- BackwardCompatibilityTests.cs - Assert.DoesNotThrow patterns
- AotCompatibilityTests.cs - Complex exception assertions
- DownloadUrlExtensionsTests.cs - Complex exception assertions
- HttpServiceTests.cs - OneTimeSetUp/TearDown with instance state
- BitmapImageExtensionsTests.cs - OneTimeSetUp/TearDown with instance state
- ISerializerInterfaceTests.cs - Many assertion conversions
- SerializerExtensionsTests.cs - Many assertion conversions
