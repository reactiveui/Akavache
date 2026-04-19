// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Download tests extracted from BlobCacheTestsBase so they run in
/// a dedicated assembly that does not compete with other parallel assemblies for
/// TCP sockets. Each test spins up its own <see cref="TestHttpServer"/> on an
/// ephemeral port.
/// </summary>
public abstract class BlobCacheDownloadTestsBase : IDisposable
{
    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tests to make sure the download url extension methods download correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DownloadUrlTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            var bytes = fixture.DownloadUrl(server.BaseUrl + "html").WaitForValue();
            await Assert.That(bytes).IsNotEmpty();
        }
    }

    /// <summary>
    /// Tests to make sure the download Uri extension method overload performs correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DownloadUriTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            Uri uri = new(server.BaseUrl + "html");
            var bytes = fixture.DownloadUrl(uri).WaitForValue();
            await Assert.That(bytes).IsNotEmpty();
        }
    }

    /// <summary>
    /// Tests to make sure the download with key extension method overload performs correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DownloadUrlWithKeyTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            var key = Guid.NewGuid().ToString();
            fixture.DownloadUrl(key, server.BaseUrl + "html").WaitForValue();

            var bytes = fixture.Get(key).WaitForValue();
            await Assert.That(bytes).IsNotEmpty();
        }
    }

    /// <summary>
    /// Tests to make sure the download Uri with key extension method overload performs correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DownloadUriWithKeyTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            var key = Guid.NewGuid().ToString();
            fixture.DownloadUrl(key, new Uri(server.BaseUrl + "html")).WaitForValue();

            var bytes = fixture.Get(key).WaitForValue();
            await Assert.That(bytes).IsNotEmpty();
        }
    }

    /// <summary>
    /// Gets the <see cref="IBlobCache"/> for testing.
    /// </summary>
    /// <param name="path">The path for disk-based caches.</param>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>The blob cache for testing.</returns>
    protected abstract IBlobCache CreateBlobCache(string path, ISerializer serializer);

    /// <summary>
    /// Releases resources.
    /// </summary>
    /// <param name="disposing">True to release managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Sets up the serializer for the test.
    /// </summary>
    /// <param name="serializerType">The type of serializer to create.</param>
    /// <returns>The created serializer instance.</returns>
    private static ISerializer SetupTestSerializer(Type? serializerType) =>
        serializerType is null
            ? throw new ArgumentNullException(nameof(serializerType))
            : (ISerializer)Activator.CreateInstance(serializerType)!;
}
