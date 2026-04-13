// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Tests for LoginInfo.
/// </summary>
[Category("Akavache")]
public class LoginInfoTests
{
    /// <summary>
    /// Tests the constructor with username and password sets the properties.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldSetProperties()
    {
        LoginInfo login = new("user1", "pass1");
        await Assert.That(login.UserName).IsEqualTo("user1");
        await Assert.That(login.Password).IsEqualTo("pass1");
    }

    /// <summary>
    /// Tests the internal constructor that takes a tuple.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TupleConstructorShouldSetProperties()
    {
        LoginInfo login = new(("user2", "pass2"));
        await Assert.That(login.UserName).IsEqualTo("user2");
        await Assert.That(login.Password).IsEqualTo("pass2");
    }

    /// <summary>
    /// Tests <see cref="LoginInfo.ToString"/> renders the username.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToStringShouldRenderUserName()
    {
        LoginInfo login = new("alice", "secret");
        await Assert.That(login.ToString()).IsEqualTo("UserName: alice");
    }

    /// <summary>
    /// Tests that <see cref="LoginInfo.Equals(object)"/> returns <see langword="false"/> for a non-<see cref="LoginInfo"/> argument.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ObjectEqualsShouldReturnFalseForDifferentType()
    {
        LoginInfo login = new("alice", "secret");
        await Assert.That(login.Equals((object)"not-a-login")).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="LoginInfo.Equals(object)"/> returns <see langword="true"/> for a matching instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ObjectEqualsShouldReturnTrueForMatchingInstance()
    {
        LoginInfo a = new("alice", "secret");
        LoginInfo b = new("alice", "secret");
        await Assert.That(a.Equals((object)b)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="LoginInfo.Equals(LoginInfo)"/> returns <see langword="false"/> for a <see langword="null"/> argument.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseForNull()
    {
        LoginInfo login = new("alice", "secret");
        await Assert.That(login.Equals(null)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="LoginInfo.Equals(LoginInfo)"/> short-circuits on reference equality.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnTrueForSameReference()
    {
        LoginInfo login = new("alice", "secret");
        await Assert.That(login.Equals(login)).IsTrue();
    }

    /// <summary>
    /// Tests that <see cref="LoginInfo.Equals(LoginInfo)"/> compares both username and password.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenUsernameDiffers()
    {
        LoginInfo a = new("alice", "secret");
        LoginInfo b = new("bob", "secret");
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="LoginInfo.Equals(LoginInfo)"/> returns <see langword="false"/> when only the password differs.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EqualsShouldReturnFalseWhenPasswordDiffers()
    {
        LoginInfo a = new("alice", "secret");
        LoginInfo b = new("alice", "different");
        await Assert.That(a.Equals(b)).IsFalse();
    }

    /// <summary>
    /// Tests that <see cref="LoginInfo.GetHashCode"/> yields the same value for equivalent instances.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetHashCodeShouldMatchForEqualInstances()
    {
        LoginInfo a = new("alice", "secret");
        LoginInfo b = new("alice", "secret");
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }
}
