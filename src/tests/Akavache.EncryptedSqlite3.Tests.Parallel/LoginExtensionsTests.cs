// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for login extension methods.
/// </summary>
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);
            const string username = "testuser";
            const string password = "testpassword";

            try
            {
                // Act - Save login
                cache.SaveLogin(username, password).WaitForCompletion();

                // Act - Get login
                var loginInfo = cache.GetLogin().WaitForValue();

                // Assert
                using (Assert.Multiple())
                {
                    await Assert.That(loginInfo).IsNotNull();
                    await Assert.That(loginInfo.UserName).IsEqualTo(username);
                    await Assert.That(loginInfo.Password).IsEqualTo(password);
                }
            }
            finally
            {
                cache.Dispose();
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);
            const string username = "customuser";
            const string password = "custompassword";
            const string host = "example.com";

            try
            {
                // Act - Save login with custom host
                cache.SaveLogin(username, password, host).WaitForCompletion();

                // Act - Get login with custom host
                var loginInfo = cache.GetLogin(host).WaitForValue();

                // Assert
                using (Assert.Multiple())
                {
                    await Assert.That(loginInfo).IsNotNull();
                    await Assert.That(loginInfo.UserName).IsEqualTo(username);
                    await Assert.That(loginInfo.Password).IsEqualTo(password);
                }
            }
            finally
            {
                cache.Dispose();
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
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);
            const string username = "expiringuser";
            const string password = "expiringpassword";
            const string host = "expiring.example.com";
            var expiration = DateTimeOffset.Now.AddHours(1);

            try
            {
                // Act - Save login with expiration
                cache.SaveLogin(username, password, host, expiration).WaitForCompletion();

                // Act - Get login before expiration
                var loginInfo = cache.GetLogin(host).WaitForValue();

                // Assert
                await Assert.That(loginInfo).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(loginInfo.UserName).IsEqualTo(username);
                    await Assert.That(loginInfo.Password).IsEqualTo(password);
                }

                // Verify the expiration was set (we can check creation time)
                var createdAt = cache.GetCreatedAt("login:" + host).WaitForValue();
                await Assert.That(createdAt).IsNotNull();
                await Assert.That(createdAt!.Value).IsLessThanOrEqualTo(DateTimeOffset.Now);
            }
            finally
            {
                cache.Dispose();
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);
            const string username = "erasableuser";
            const string password = "erasablepassword";
            const string host = "erasable.example.com";

            try
            {
                // Act - Save login
                cache.SaveLogin(username, password, host).WaitForCompletion();

                // Verify login exists
                var loginInfo = cache.GetLogin(host).WaitForValue();
                await Assert.That(loginInfo).IsNotNull();
                await Assert.That(loginInfo.UserName).IsEqualTo(username);

                // Act - Erase login
                cache.EraseLogin(host).WaitForCompletion();

                // Assert - Login should no longer exist
                var getError = cache.GetLogin(host).WaitForError();
                await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that GetLogin throws <see cref="KeyNotFoundException"/> when the stored
    /// entry deserializes to a null <see cref="LoginInfo"/>. This specifically
    /// exercises the null branch of the <c>x ?? throw</c> coalesce operator inside
    /// <see cref="LoginExtensions.GetLogin"/> — the upstream cache miss path throws
    /// before reaching the Select, leaving that branch uncovered otherwise.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetLoginShouldThrowKeyNotFoundExceptionWhenStoredValueIsNull()
    {
        SystemJsonSerializer serializer = new();
        const string host = "null-login-host";
        const string key = "login:" + host;

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Writing an empty byte[] under the typed key causes GetObject<LoginInfo>
        // to emit a null value (it interprets empty payloads as stored nulls) so
        // the null branch of LoginExtensions.GetLogin's Select throw runs.
        cache.Insert(key, [], typeof(LoginInfo)).SubscribeAndComplete();

        var getError = cache.GetLogin(host).SubscribeGetError();
        await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that GetLogin throws KeyNotFoundException when no login exists.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetLoginShouldThrowKeyNotFoundExceptionWhenNoLoginExists()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);
            const string host = "nonexistent.example.com";

            try
            {
                // Act & Assert
                var getError = cache.GetLogin(host).WaitForError();
                await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);

            const string host1 = "site1.example.com";
            const string user1 = "user1";
            const string pass1 = "password1";

            const string host2 = "site2.example.com";
            const string user2 = "user2";
            const string pass2 = "password2";

            try
            {
                // Act - Save different credentials for different hosts
                cache.SaveLogin(user1, pass1, host1).WaitForCompletion();
                cache.SaveLogin(user2, pass2, host2).WaitForCompletion();

                // Act - Get credentials for each host
                var login1 = cache.GetLogin(host1).WaitForValue();
                var login2 = cache.GetLogin(host2).WaitForValue();

                // Assert - Each host should have its own credentials
                await Assert.That(login1).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(login1.UserName).IsEqualTo(user1);
                    await Assert.That(login1.Password).IsEqualTo(pass1);

                    await Assert.That(login2).IsNotNull();
                }

                using (Assert.Multiple())
                {
                    await Assert.That(login2.UserName).IsEqualTo(user2);
                    await Assert.That(login2.Password).IsEqualTo(pass2);
                }

                using (Assert.Multiple())
                {
                    // Verify they are different
                    await Assert.That(login2.UserName).IsNotEqualTo(login1.UserName);
                    await Assert.That(login2.Password).IsNotEqualTo(login1.Password);
                }
            }
            finally
            {
                cache.Dispose();
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);
            const string host = "overwrite.example.com";

            const string originalUser = "originaluser";
            const string originalPass = "originalpass";

            const string newUser = "newuser";
            const string newPass = "newpass";

            try
            {
                // Act - Save original credentials
                cache.SaveLogin(originalUser, originalPass, host).WaitForCompletion();

                // Verify original credentials
                var originalLogin = cache.GetLogin(host).WaitForValue();
                using (Assert.Multiple())
                {
                    await Assert.That(originalLogin!.UserName).IsEqualTo(originalUser);
                    await Assert.That(originalLogin!.Password).IsEqualTo(originalPass);
                }

                // Act - Overwrite with new credentials
                cache.SaveLogin(newUser, newPass, host).WaitForCompletion();

                // Assert - Should get new credentials, not original
                var newLogin = cache.GetLogin(host).WaitForValue();
                await Assert.That(newLogin).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(newLogin.UserName).IsEqualTo(newUser);
                    await Assert.That(newLogin.Password).IsEqualTo(newPass);
                }

                using (Assert.Multiple())
                {
                    // Verify old credentials are gone
                    await Assert.That(newLogin.UserName).IsNotEqualTo(originalUser);
                    await Assert.That(newLogin.Password).IsNotEqualTo(originalPass);
                }
            }
            finally
            {
                cache.Dispose();
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "persistent_login_test.db");
            const string username = "persistentuser";
            const string password = "persistentpassword";
            const string host = "persistent.example.com";

            // Act - Save credentials in first cache instance
            {
                EncryptedSqliteBlobCache cache1 = new(dbPath, "test_password", serializer);
                try
                {
                    cache1.SaveLogin(username, password, host).WaitForCompletion();
                    cache1.Flush().WaitForCompletion();
                }
                finally
                {
                    cache1.Dispose();
                    await Task.Delay(100); // Allow cleanup
                }
            }

            // Act - Retrieve credentials in second cache instance
            {
                EncryptedSqliteBlobCache cache2 = new(dbPath, "test_password", serializer);
                try
                {
                    var loginInfo = cache2.GetLogin(host).WaitForValue();

                    // Assert
                    await Assert.That(loginInfo).IsNotNull();
                    using (Assert.Multiple())
                    {
                        await Assert.That(loginInfo.UserName).IsEqualTo(username);
                        await Assert.That(loginInfo.Password).IsEqualTo(password);
                    }
                }
                finally
                {
                    cache2.Dispose();
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);

            try
            {
                // Test with empty strings (should be allowed)
                cache.SaveLogin(string.Empty, string.Empty, "empty.example.com").WaitForCompletion();
                var emptyLogin = cache.GetLogin("empty.example.com").WaitForValue();
                await Assert.That(emptyLogin).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(emptyLogin!.UserName).IsEqualTo(string.Empty);
                    await Assert.That(emptyLogin.Password).IsEqualTo(string.Empty);
                }

                // Test with whitespace
                cache.SaveLogin("  ", "  ", "whitespace.example.com").WaitForCompletion();
                var whitespaceLogin = cache.GetLogin("whitespace.example.com").WaitForValue();
                await Assert.That(whitespaceLogin).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(whitespaceLogin!.UserName).IsEqualTo("  ");
                    await Assert.That(whitespaceLogin.Password).IsEqualTo("  ");
                }

                // Test with special characters
                const string specialUser = "user@domain.com";
                const string specialPass = "p@ssw0rd!#$%";
                cache.SaveLogin(specialUser, specialPass, "special.example.com").WaitForCompletion();
                var specialLogin = cache.GetLogin("special.example.com").WaitForValue();
                await Assert.That(specialLogin).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(specialLogin!.UserName).IsEqualTo(specialUser);
                    await Assert.That(specialLogin.Password).IsEqualTo(specialPass);
                }
            }
            finally
            {
                cache.Dispose();
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
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out var path))
        {
            EncryptedSqliteBlobCache cache = new(Path.Combine(path, "login_test.db"), "test_password", serializer);
            const string host = "idempotent.example.com";

            try
            {
                // Save a login first
                cache.SaveLogin("testuser", "testpass", host).WaitForCompletion();

                // Erase it once
                cache.EraseLogin(host).WaitForCompletion();

                // Erase it again - should not throw
                cache.EraseLogin(host).WaitForCompletion();

                // Erase a non-existent login - should not throw
                cache.EraseLogin("nonexistent.example.com").WaitForCompletion();

                // Verify the login is still gone
                var getError = cache.GetLogin(host).WaitForError();
                await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }
}
