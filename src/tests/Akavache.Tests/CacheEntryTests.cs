// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="CacheEntry"/>.
/// </summary>
[Category("Akavache")]
public class CacheEntryTests
{
    /// <summary>
    /// Tests that <see cref="CacheEntry.ToString"/> renders all relevant fields.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToStringShouldRenderFields()
    {
        var created = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var expires = new DateTimeOffset(2026, 2, 2, 3, 4, 5, TimeSpan.Zero);
        CacheEntry entry = new()
        {
            Id = "key-1",
            CreatedAt = created,
            ExpiresAt = expires,
            TypeName = "MyType",
        };

        var text = entry.ToString();

        await Assert.That(text).Contains("Id: key-1");
        await Assert.That(text).Contains("Type: MyType");
        await Assert.That(text).Contains("Created: " + created);
        await Assert.That(text).Contains("Expires: " + expires);
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(object)"/> returns <see langword="false"/> for a non-<see cref="CacheEntry"/> argument.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ObjectEqualsShouldReturnFalseForDifferentType()
    {
        CacheEntry entry = new() { Id = "k" };
        await Assert.That(entry.Equals((object)"not-a-cache-entry")).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(object)"/> returns <see langword="true"/> for a matching instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ObjectEqualsShouldReturnTrueForMatchingInstance()
    {
        CacheEntry a = new() { Id = "k", TypeName = "t" };
        CacheEntry b = new() { Id = "k", TypeName = "t" };
        await Assert.That(a.Equals((object)b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> short-circuits on reference equality.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnTrueForSameReference()
    {
        CacheEntry entry = new() { Id = "k" };
        await Assert.That(entry.Equals(entry)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.Id"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenIdDiffers()
    {
        CacheEntry a = new() { Id = "k1" };
        CacheEntry b = new() { Id = "k2" };
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.CreatedAt"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenCreatedAtDiffers()
    {
        CacheEntry a = new() { Id = "k", CreatedAt = DateTimeOffset.UnixEpoch };
        CacheEntry b = new() { Id = "k", CreatedAt = DateTimeOffset.UnixEpoch.AddSeconds(1) };
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.ExpiresAt"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenExpiresAtDiffers()
    {
        CacheEntry a = new() { Id = "k", ExpiresAt = DateTimeOffset.UnixEpoch };
        CacheEntry b = new() { Id = "k", ExpiresAt = null };
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.TypeName"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenTypeNameDiffers()
    {
        CacheEntry a = new() { Id = "k", TypeName = "t1" };
        CacheEntry b = new() { Id = "k", TypeName = "t2" };
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> compares the <see cref="CacheEntry.Value"/> byte arrays by content.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnTrueForEqualValueBytes()
    {
        CacheEntry a = new() { Id = "k", Value = [1, 2, 3] };
        CacheEntry b = new() { Id = "k", Value = [1, 2, 3] };
        await Assert.That(a.Equals(b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> returns <see langword="false"/> when the <see cref="CacheEntry.Value"/> content differs.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenValueBytesDiffer()
    {
        CacheEntry a = new() { Id = "k", Value = [1, 2, 3] };
        CacheEntry b = new() { Id = "k", Value = [1, 2, 4] };
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> returns <see langword="false"/> when one <see cref="CacheEntry.Value"/> is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenOneValueIsNull()
    {
        CacheEntry a = new() { Id = "k", Value = [1, 2, 3] };
        CacheEntry b = new() { Id = "k", Value = null };
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> returns <see langword="true"/> when both <see cref="CacheEntry.Value"/> arrays are <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnTrueWhenBothValuesAreNull()
    {
        CacheEntry a = new() { Id = "k", Value = null };
        CacheEntry b = new() { Id = "k", Value = null };
        await Assert.That(a.Equals(b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.GetHashCode"/> yields the same hash for equal instances including matching value bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetHashCodeShouldMatchForEqualInstancesWithValueBytes()
    {
        CacheEntry a = new() { Id = "k", TypeName = "t", Value = [9, 8, 7] };
        CacheEntry b = new() { Id = "k", TypeName = "t", Value = [9, 8, 7] };
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.GetHashCode"/> yields the same hash for equal instances with <see langword="null"/> value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetHashCodeShouldMatchForEqualInstancesWithNullValue()
    {
        CacheEntry a = new() { Id = "k", TypeName = "t", Value = null };
        CacheEntry b = new() { Id = "k", TypeName = "t", Value = null };
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    /// <summary>
    /// Tests that the parameterised constructor populates every field verbatim.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ParameterisedConstructorShouldPopulateAllFields()
    {
        var created = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var expires = new DateTimeOffset(2026, 4, 5, 6, 7, 8, TimeSpan.Zero);
        byte[] payload = [1, 2, 3];

        var entry = new CacheEntry("my-key", "My.Type", payload, created, expires);

        await Assert.That(entry.Id).IsEqualTo("my-key");
        await Assert.That(entry.TypeName).IsEqualTo("My.Type");
        await Assert.That(entry.Value).IsEqualTo(payload);
        await Assert.That(entry.CreatedAt).IsEqualTo(created);
        await Assert.That(entry.ExpiresAt).IsEqualTo(expires);
    }

    /// <summary>
    /// Tests that the parameterised constructor accepts null typeName, null value, and null expiry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ParameterisedConstructorShouldAcceptNullOptionalFields()
    {
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var entry = new CacheEntry("key", typeName: null, value: null, created, expiresAt: null);

        await Assert.That(entry.Id).IsEqualTo("key");
        await Assert.That(entry.TypeName).IsNull();
        await Assert.That(entry.Value).IsNull();
        await Assert.That(entry.ExpiresAt).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.ValueEquals"/> returns true when both buffers are the same reference.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueEqualsShouldReturnTrueForSameReference()
    {
        byte[] buf = [1, 2, 3];
        await Assert.That(CacheEntry.ValueEquals(buf, buf)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.ValueEquals"/> returns true when both buffers are null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueEqualsShouldReturnTrueForBothNull() =>
        await Assert.That(CacheEntry.ValueEquals(null, null)).IsTrue();

    /// <summary>
    /// Tests that <see cref="CacheEntry.ValueEquals"/> returns false when exactly one buffer is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueEqualsShouldReturnFalseWhenOnlyOneIsNull()
    {
        byte[] buf = [1];
        await Assert.That(CacheEntry.ValueEquals(buf, null)).IsFalse();
        await Assert.That(CacheEntry.ValueEquals(null, buf)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.ValueEquals"/> returns true for distinct buffers with identical contents.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueEqualsShouldReturnTrueForDistinctBuffersWithEqualContent()
    {
        byte[] a = [1, 2, 3, 4, 5];
        byte[] b = [1, 2, 3, 4, 5];
        await Assert.That(CacheEntry.ValueEquals(a, b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.ValueEquals"/> returns false for buffers of equal length but differing content.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueEqualsShouldReturnFalseForEqualLengthDifferentContent()
    {
        byte[] a = [1, 2, 3];
        byte[] b = [1, 2, 4];
        await Assert.That(CacheEntry.ValueEquals(a, b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.ValueEquals"/> returns false for buffers of different length.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueEqualsShouldReturnFalseForDifferentLengths()
    {
        byte[] a = [1, 2, 3];
        byte[] b = [1, 2];
        await Assert.That(CacheEntry.ValueEquals(a, b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.ValueEquals"/> treats two empty buffers as equal.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueEqualsShouldReturnTrueForTwoEmptyBuffers() =>
        await Assert.That(CacheEntry.ValueEquals([], [])).IsTrue();
}
