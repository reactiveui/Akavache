// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests covering the argument guards on the serializer extensions.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public partial class SerializerExtensionsTests
{
    /// <summary>Tests InsertObjects throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertObjects<string>(null!, [
                new("k", "v")
            ]))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetObjects throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetObjects<string>(null!, ["k"]))
            .Throws<ArgumentNullException>();

    /// <summary>Tests InsertObject throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertObject(null!, "key", SingleEntryValue))
            .Throws<ArgumentNullException>();

    /// <summary>Tests InsertObject throws on empty key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.InsertObject(cache, string.Empty, SingleEntryValue))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InsertObject handles null value by storing empty bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldHandleNullValue()
    {
        var cache = CreateCache();
        try
        {
            _ = cache.InsertObject<string>("k", null!).Subscribe();
            string? result = null;
            _ = cache.GetObject<string>("k").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests GetObject throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetObject<string>(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetObject throws on empty key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.GetObject<string>(cache, string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests GetAllObjects throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllObjects<string>(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetObjectCreatedAt throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetObjectCreatedAt<string>(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetObjectCreatedAt throws on empty key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.GetObjectCreatedAt<string>(cache, string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InvalidateObject throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InvalidateObject<string>(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests InvalidateObject throws on empty key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.InvalidateObject<string>(cache, string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InvalidateObjects throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InvalidateObjects<string>(null!, ["key"]))
            .Throws<ArgumentNullException>();

    /// <summary>Tests InvalidateObjects throws on null keys.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectsShouldThrowOnNullKeys()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.InvalidateObjects<string>(null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InvalidateAllObjects throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InvalidateAllObjects<string>(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Exercises the static <see cref="SerializerExtensions.GetAllObjects{T}"/> extension
    /// method directly. Tests that use <c>cache.GetAllObjects&lt;T&gt;()</c> on an
    /// <see cref="InMemoryBlobCache"/> actually hit the shadowing instance method on
    /// <see cref="InMemoryBlobCacheBase"/>, so the extension body never executes. This
    /// test invokes the extension explicitly via its static form on the
    /// <see cref="IBlobCache"/> interface so the extension method body is covered.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAllObjectsStaticExtensionShouldReturnStoredObjects()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        try
        {
            UserObject user1 = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };
            UserObject user2 = new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog };
            cache.InsertObject(FirstUserKey, user1).WaitForCompletion();
            cache.InsertObject(SecondUserKey, user2).WaitForCompletion();

            var results = await SerializerExtensions.GetAllObjects<UserObject>(cache).ToList();

            await Assert.That(results).Count().IsEqualTo(SampleUserCount);
            await Assert.That(results.Any(static x => x.Name == FirstUserName)).IsTrue();
            await Assert.That(results.Any(static x => x.Name == SecondUserName)).IsTrue();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Exercises the static <see cref="SerializerExtensions.InvalidateAllObjects{T}"/>
    /// extension method directly. See <see cref="GetAllObjectsStaticExtensionShouldReturnStoredObjects"/>
    /// for why the static form is necessary.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateAllObjectsStaticExtensionShouldRemoveAllObjectsOfType()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        try
        {
            UserObject user1 = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };
            cache.InsertObject(FirstUserKey, user1).WaitForCompletion();

            _ = SerializerExtensions.InvalidateAllObjects<UserObject>(cache).Subscribe();

            var results = await SerializerExtensions.GetAllObjects<UserObject>(cache).ToList();
            await Assert.That(results).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InsertAllObjects throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertAllObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertAllObjects<string>(null!, [
                new("k", "v")
            ]))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetOrFetchObject throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldThrowOnNullCache() =>
        await Assert.That(static () =>
                SerializerExtensions.GetOrFetchObject(null!, "key", static () => Signal.Return(SingleEntryValue)))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetOrFetchObject throws on null fetchFunc.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldThrowOnNullFetchFunc()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.GetOrFetchObject("key", (Func<IObservable<string>>)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InsertObjects(IDictionary) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertObjects(null!, new Dictionary<string, object>()))
            .Throws<ArgumentNullException>();

    /// <summary>Tests InsertObjects(IDictionary) throws on null keyValuePairs.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldThrowOnNullPairs()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.InsertObjects(null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InsertObjects(IDictionary) returns immediately for empty input.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldReturnImmediatelyForEmpty()
    {
        var cache = CreateCache();
        try
        {
            _ = cache.InsertObjects(new Dictionary<string, object>()).Subscribe();
            IList<string>? keys = null;
            _ = cache.GetAllKeys().ToList().Subscribe(v => keys = v);
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests InsertObjects(IDictionary) inserts mixed types.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldInsertMixedTypes()
    {
        var cache = CreateCache();
        try
        {
            Dictionary<string, object> data = new() { ["k1"] = "string value", ["k2"] = DictionaryIntValue, ["k3"] = new UserObject { Name = "user", Bio = "bio", Blog = "blog" } };

            _ = cache.InsertObjects(data).Subscribe();

            IList<string>? keys = null;
            _ = cache.GetAllKeys().ToList().Subscribe(v => keys = v);
            await Assert.That(keys!.Count).IsEqualTo(DictionaryKeyCount);
        }
        finally
        {
            cache.Dispose();
        }
    }
}
