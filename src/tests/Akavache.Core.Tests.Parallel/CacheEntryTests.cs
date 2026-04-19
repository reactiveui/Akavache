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
    /// Tests that <see cref="CacheEntry.ToString"/> renders all relevant fields via the
    /// record's synthesized <c>ToString</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToStringShouldRenderFields()
    {
        var created = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var expires = new DateTimeOffset(2026, 2, 2, 3, 4, 5, TimeSpan.Zero);
        CacheEntry entry = new("key-1", "MyType", Value: null, created, expires);

        var text = entry.ToString();

        await Assert.That(text).Contains("Id = key-1");
        await Assert.That(text).Contains("TypeName = MyType");
        await Assert.That(text).Contains("CreatedAt = " + created);
        await Assert.That(text).Contains("ExpiresAt = " + expires);
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(object)"/> returns <see langword="false"/> for a non-<see cref="CacheEntry"/> argument.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ObjectEqualsShouldReturnFalseForDifferentType()
    {
        CacheEntry entry = new("k", TypeName: null, Value: null, default, ExpiresAt: null);
        await Assert.That(entry.Equals((object)"not-a-cache-entry")).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(object)"/> returns <see langword="true"/> for a matching instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ObjectEqualsShouldReturnTrueForMatchingInstance()
    {
        CacheEntry a = new("k", "t", Value: null, default, ExpiresAt: null);
        CacheEntry b = new("k", "t", Value: null, default, ExpiresAt: null);
        await Assert.That(a.Equals((object)b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> short-circuits on reference equality.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnTrueForSameReference()
    {
        CacheEntry entry = new("k", TypeName: null, Value: null, default, ExpiresAt: null);
        await Assert.That(entry.Equals(entry)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.Id"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenIdDiffers()
    {
        CacheEntry a = new("k1", TypeName: null, Value: null, default, ExpiresAt: null);
        CacheEntry b = new("k2", TypeName: null, Value: null, default, ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.CreatedAt"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenCreatedAtDiffers()
    {
        CacheEntry a = new("k", TypeName: null, Value: null, DateTimeOffset.UnixEpoch, ExpiresAt: null);
        CacheEntry b = new("k", TypeName: null, Value: null, DateTimeOffset.UnixEpoch.AddSeconds(1), ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.ExpiresAt"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenExpiresAtDiffers()
    {
        CacheEntry a = new("k", TypeName: null, Value: null, default, DateTimeOffset.UnixEpoch);
        CacheEntry b = new("k", TypeName: null, Value: null, default, ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> checks the <see cref="CacheEntry.TypeName"/> field.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenTypeNameDiffers()
    {
        CacheEntry a = new("k", "t1", Value: null, default, ExpiresAt: null);
        CacheEntry b = new("k", "t2", Value: null, default, ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> compares the <see cref="CacheEntry.Value"/> byte arrays by content.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnTrueForEqualValueBytes()
    {
        CacheEntry a = new("k", TypeName: null, [1, 2, 3], default, ExpiresAt: null);
        CacheEntry b = new("k", TypeName: null, [1, 2, 3], default, ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> returns <see langword="false"/> when the <see cref="CacheEntry.Value"/> content differs.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenValueBytesDiffer()
    {
        CacheEntry a = new("k", TypeName: null, [1, 2, 3], default, ExpiresAt: null);
        CacheEntry b = new("k", TypeName: null, [1, 2, 4], default, ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> returns <see langword="false"/> when one <see cref="CacheEntry.Value"/> is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenOneValueIsNull()
    {
        CacheEntry a = new("k", TypeName: null, [1, 2, 3], default, ExpiresAt: null);
        CacheEntry b = new("k", TypeName: null, Value: null, default, ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.Equals(CacheEntry)"/> returns <see langword="true"/> when both <see cref="CacheEntry.Value"/> arrays are <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnTrueWhenBothValuesAreNull()
    {
        CacheEntry a = new("k", TypeName: null, Value: null, default, ExpiresAt: null);
        CacheEntry b = new("k", TypeName: null, Value: null, default, ExpiresAt: null);
        await Assert.That(a.Equals(b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.GetHashCode"/> yields the same hash for equal instances including matching value bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetHashCodeShouldMatchForEqualInstancesWithValueBytes()
    {
        CacheEntry a = new("k", "t", [9, 8, 7], default, ExpiresAt: null);
        CacheEntry b = new("k", "t", [9, 8, 7], default, ExpiresAt: null);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    /// <summary>
    /// Tests that <see cref="CacheEntry.GetHashCode"/> yields the same hash for equal instances with <see langword="null"/> value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetHashCodeShouldMatchForEqualInstancesWithNullValue()
    {
        CacheEntry a = new("k", "t", Value: null, default, ExpiresAt: null);
        CacheEntry b = new("k", "t", Value: null, default, ExpiresAt: null);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    /// <summary>
    /// Tests that the positional constructor populates every field verbatim.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PositionalConstructorShouldPopulateAllFields()
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
    /// Tests that the positional constructor accepts null typeName, null value, and null expiry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PositionalConstructorShouldAcceptNullOptionalFields()
    {
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var entry = new CacheEntry("key", TypeName: null, Value: null, created, ExpiresAt: null);

        await Assert.That(entry.Id).IsEqualTo("key");
        await Assert.That(entry.TypeName).IsNull();
        await Assert.That(entry.Value).IsNull();
        await Assert.That(entry.ExpiresAt).IsNull();
    }
}
