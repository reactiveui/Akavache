// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for login extension methods.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class LoginExtensionsTests
{
    /// <summary>
    /// Tests that SaveLogin and GetLogin work correctly with default host.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveLoginAndGetLoginShouldWorkWithDefaultHost()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);
            var username = "testuser";
            var password = "testpassword";

            try
            {
                // Act - Save login
                await cache.SaveLogin(username, password).FirstAsync();

                // Act - Get login
                var loginInfo = await cache.GetLogin().FirstAsync();

                // Assert
                Assert.Multiple(() =>
                {
                    Assert.That(loginInfo, Is.Not.Null);
                    Assert.That(loginInfo.UserName, Is.EqualTo(username));
                    Assert.That(loginInfo.Password, Is.EqualTo(password));
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that SaveLogin and GetLogin work correctly with custom host.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveLoginAndGetLoginShouldWorkWithCustomHost()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);
            var username = "customuser";
            var password = "custompassword";
            var host = "example.com";

            try
            {
                // Act - Save login with custom host
                await cache.SaveLogin(username, password, host).FirstAsync();

                // Act - Get login with custom host
                var loginInfo = await cache.GetLogin(host).FirstAsync();

                // Assert
                Assert.Multiple(() =>
                {
                    Assert.That(loginInfo, Is.Not.Null);
                    Assert.That(loginInfo.UserName, Is.EqualTo(username));
                    Assert.That(loginInfo.Password, Is.EqualTo(password));
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that SaveLogin with expiration works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveLoginWithExpirationShouldWork()
    {
        // Use a single serializer instance to avoid issues
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);
            var username = "expiringuser";
            var password = "expiringpassword";
            var host = "expiring.example.com";
            var expiration = DateTimeOffset.Now.AddHours(1);

            try
            {
                // Act - Save login with expiration
                await cache.SaveLogin(username, password, host, expiration).FirstAsync();

                // Act - Get login before expiration
                var loginInfo = await cache.GetLogin(host).FirstAsync();

                // Assert
                Assert.That(loginInfo, Is.Not.Null);
                Assert.That(loginInfo.UserName, Is.EqualTo(username));
                Assert.That(loginInfo.Password, Is.EqualTo(password));

                // Verify the expiration was set (we can check creation time)
                var createdAt = await cache.GetCreatedAt("login:" + host).FirstAsync();
                Assert.That(createdAt, Is.Not.Null);
                Assert.That(createdAt, Is.LessThanOrEqualTo(DateTimeOffset.Now));
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that EraseLogin removes login correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task EraseLoginShouldRemoveLoginCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);
            var username = "erasableuser";
            var password = "erasablepassword";
            var host = "erasable.example.com";

            try
            {
                // Act - Save login
                await cache.SaveLogin(username, password, host).FirstAsync();

                // Verify login exists
                var loginInfo = await cache.GetLogin(host).FirstAsync();
                Assert.That(loginInfo, Is.Not.Null);
                Assert.That(loginInfo.UserName, Is.EqualTo(username));

                // Act - Erase login
                await cache.EraseLogin(host).FirstAsync();

                // Assert - Login should no longer exist
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.GetLogin(host).FirstAsync();
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that GetLogin throws KeyNotFoundException when no login exists.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetLoginShouldThrowKeyNotFoundExceptionWhenNoLoginExists()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);
            var host = "nonexistent.example.com";

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.GetLogin(host).FirstAsync();
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that multiple hosts can have different login credentials.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task MultipleHostsShouldHaveDifferentCredentials()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);

            var host1 = "site1.example.com";
            var user1 = "user1";
            var pass1 = "password1";

            var host2 = "site2.example.com";
            var user2 = "user2";
            var pass2 = "password2";

            try
            {
                // Act - Save different credentials for different hosts
                await cache.SaveLogin(user1, pass1, host1).FirstAsync();
                await cache.SaveLogin(user2, pass2, host2).FirstAsync();

                // Act - Get credentials for each host
                var login1 = await cache.GetLogin(host1).FirstAsync();
                var login2 = await cache.GetLogin(host2).FirstAsync();

                // Assert - Each host should have its own credentials
                Assert.That(login1, Is.Not.Null);
                Assert.That(login1.UserName, Is.EqualTo(user1));
                Assert.That(login1.Password, Is.EqualTo(pass1));

                Assert.That(login2, Is.Not.Null);
                Assert.That(login2.UserName, Is.EqualTo(user2));
                Assert.That(login2.Password, Is.EqualTo(pass2));

                // Verify they are different
                Assert.That(login2.UserName, Is.Not.EqualTo(login1.UserName));
                Assert.That(login2.Password, Is.Not.EqualTo(login1.Password));
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that SaveLogin overwrites previous credentials for the same host.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveLoginShouldOverwritePreviousCredentials()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);
            var host = "overwrite.example.com";

            var originalUser = "originaluser";
            var originalPass = "originalpass";

            var newUser = "newuser";
            var newPass = "newpass";

            try
            {
                // Act - Save original credentials
                await cache.SaveLogin(originalUser, originalPass, host).FirstAsync();

                // Verify original credentials
                var originalLogin = await cache.GetLogin(host).FirstAsync();
                Assert.That(originalLogin.UserName, Is.EqualTo(originalUser));
                Assert.That(originalLogin.Password, Is.EqualTo(originalPass));

                // Act - Overwrite with new credentials
                await cache.SaveLogin(newUser, newPass, host).FirstAsync();

                // Assert - Should get new credentials, not original
                var newLogin = await cache.GetLogin(host).FirstAsync();
                Assert.That(newLogin, Is.Not.Null);
                Assert.That(newLogin.UserName, Is.EqualTo(newUser));
                Assert.That(newLogin.Password, Is.EqualTo(newPass));

                // Verify old credentials are gone
                Assert.That(newLogin.UserName, Is.Not.EqualTo(originalUser));
                Assert.That(newLogin.Password, Is.Not.EqualTo(originalPass));
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that login credentials persist across cache instances.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoginCredentialsShouldPersistAcrossCacheInstances()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "persistent_login_test.db");
            var username = "persistentuser";
            var password = "persistentpassword";
            var host = "persistent.example.com";

            // Act - Save credentials in first cache instance
            {
                var cache1 = new EncryptedSqliteBlobCache(dbPath, "test_password", serializer);
                try
                {
                    await cache1.SaveLogin(username, password, host).FirstAsync();
                    await cache1.Flush().FirstAsync();
                }
                finally
                {
                    await cache1.DisposeAsync();
                    await Task.Delay(100); // Allow cleanup
                }
            }

            // Act - Retrieve credentials in second cache instance
            {
                var cache2 = new EncryptedSqliteBlobCache(dbPath, "test_password", serializer);
                try
                {
                    var loginInfo = await cache2.GetLogin(host).FirstAsync();

                    // Assert
                    Assert.That(loginInfo, Is.Not.Null);
                    Assert.That(loginInfo.UserName, Is.EqualTo(username));
                    Assert.That(loginInfo.Password, Is.EqualTo(password));
                }
                finally
                {
                    await cache2.DisposeAsync();
                }
            }
        }
    }

    /// <summary>
    /// Tests that login methods handle null and empty values appropriately.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoginMethodsShouldHandleEdgeCases()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);

            try
            {
                // Test with empty strings (should be allowed)
                await cache.SaveLogin(string.Empty, string.Empty, "empty.example.com").FirstAsync();
                var emptyLogin = await cache.GetLogin("empty.example.com").FirstAsync();
                Assert.That(emptyLogin.UserName, Is.EqualTo(string.Empty));
                Assert.That(emptyLogin.Password, Is.EqualTo(string.Empty));

                // Test with whitespace
                await cache.SaveLogin("  ", "  ", "whitespace.example.com").FirstAsync();
                var whitespaceLogin = await cache.GetLogin("whitespace.example.com").FirstAsync();
                Assert.That(whitespaceLogin.UserName, Is.EqualTo("  "));
                Assert.That(whitespaceLogin.Password, Is.EqualTo("  "));

                // Test with special characters
                var specialUser = "user@domain.com";
                var specialPass = "p@ssw0rd!#$%";
                await cache.SaveLogin(specialUser, specialPass, "special.example.com").FirstAsync();
                var specialLogin = await cache.GetLogin("special.example.com").FirstAsync();
                Assert.That(specialLogin.UserName, Is.EqualTo(specialUser));
                Assert.That(specialLogin.Password, Is.EqualTo(specialPass));
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that EraseLogin is idempotent (can be called multiple times safely).
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task EraseLoginShouldBeIdempotent()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new EncryptedSqliteBlobCache(Path.Combine(path, "login_test.db"), "test_password", serializer);
            var host = "idempotent.example.com";

            try
            {
                // Save a login first
                await cache.SaveLogin("testuser", "testpass", host).FirstAsync();

                // Erase it once
                await cache.EraseLogin(host).FirstAsync();

                // Erase it again - should not throw
                await cache.EraseLogin(host).FirstAsync();

                // Erase a non-existent login - should not throw
                await cache.EraseLogin("nonexistent.example.com").FirstAsync();

                // Verify the login is still gone
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.GetLogin(host).FirstAsync();
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }
}
