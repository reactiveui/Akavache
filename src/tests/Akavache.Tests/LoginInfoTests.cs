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
}
