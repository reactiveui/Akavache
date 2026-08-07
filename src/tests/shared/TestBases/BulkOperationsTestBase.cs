// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.TestBases;
#else
namespace Akavache.Tests.TestBases;
#endif

/// <summary>A base class for tests about bulk operations.</summary>
public abstract class BulkOperationsTestBase : IDisposable
{
    /// <summary>A backing field which indicates if the class has been disposed.</summary>
    private bool _disposed;

    /// <summary>Tests if Get with multiple keys work correctly.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task GetShouldWorkWithMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            foreach (var v in keys)
            {
                fixture.Insert(v, data).WaitForCompletion();
            }

            var allKeys = fixture.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(allKeys).Count().IsEqualTo(keys.Length);

            var allData = fixture.Get(keys).ToList().SubscribeGetValue();

            await Assert.That(allData).Count().IsEqualTo(keys.Length);
            await Assert.That(allData!.All(x => x.Value[0] == data[0] && x.Value[1] == data[1])).IsTrue();
        }
    }

    /// <summary>Tests to make sure that Get invalidates all the old keys.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task GetShouldInvalidateOldKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            foreach (var v in keys)
            {
                fixture.Insert(v, data, DateTimeOffset.MinValue).WaitForCompletion();
            }

            var allData = fixture.Get(keys).ToList().SubscribeGetValue();
            using (Assert.Multiple())
            {
                await Assert.That(allData).IsEmpty();

                var remainingKeys = fixture.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(remainingKeys).IsEmpty();
            }
        }
    }

    /// <summary>Tests to make sure that insert works with multiple keys.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InsertShouldWorkWithMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            fixture.Insert(keys.ToDictionary(static k => k, _ => data)).WaitForCompletion();

            var allKeys = fixture.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(allKeys).Count().IsEqualTo(keys.Length);

            var allData = fixture.Get(keys).ToList().SubscribeGetValue();

            await Assert.That(allData).Count().IsEqualTo(keys.Length);
            await Assert.That(allData!.All(x => x.Value[0] == data[0] && x.Value[1] == data[1])).IsTrue();
        }
    }

    /// <summary>Invalidate should be able to trash multiple keys.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InvalidateShouldTrashMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            foreach (var v in keys)
            {
                fixture.Insert(v, data).WaitForCompletion();
            }

            var allKeys = fixture.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(allKeys).Count().IsEqualTo(keys.Length);

            fixture.Invalidate(keys).WaitForCompletion();

            var remainingKeys = fixture.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(remainingKeys).IsEmpty();
        }
    }

    /// <summary>Disposes the test base, restoring the original serializer.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the <see cref="IBlobCache" /> we want to do the tests against.</summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>
    /// The blob cache for testing.
    /// </returns>
    protected abstract IBlobCache CreateBlobCache(string path, ISerializer serializer);

    /// <summary>Disposes resources.</summary>
    /// <param name="disposing">True to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    /// <summary>Sets up the test with the specified serializer type.</summary>
    /// <param name="serializerType">The type of serializer to use for this test.</param>
    /// <returns>The configured serializer instance.</returns>
    private static ISerializer SetupTestSerializer(Type? serializerType)
    {
        // Clear any existing in-flight requests to ensure clean test state
        RequestCache.Clear();

        if (serializerType == typeof(NewtonsoftBsonSerializer))
        {
            // Register the Newtonsoft BSON serializer specifically
            return new NewtonsoftBsonSerializer();
        }

        if (serializerType == typeof(SystemJsonBsonSerializer))
        {
            // Register the System.Text.Json BSON serializer specifically
            return new SystemJsonBsonSerializer();
        }

        if (serializerType == typeof(NewtonsoftSerializer))
        {
            // Register the Newtonsoft JSON serializer
            return new NewtonsoftSerializer();
        }

        return serializerType == typeof(SystemJsonSerializer) ? new SystemJsonSerializer() : null!;
    }
}
