// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Akavache;

/// <summary>
/// Stored login information for a user.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoginInfo"/> class.
/// </remarks>
/// <param name="username">The username for the entry.</param>
/// <param name="password">The password for the user.</param>
[DebuggerDisplay("UserName: {UserName}")]
public class LoginInfo(string username, string password) : IEquatable<LoginInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginInfo"/> class.
    /// </summary>
    /// <param name="usernameAndLogin">A username and password stored in a tuple.</param>
    internal LoginInfo((string UserName, string Password) usernameAndLogin)
        : this(usernameAndLogin.UserName, usernameAndLogin.Password)
    {
    }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string UserName { get; } = username;

    /// <summary>
    /// Gets the password.
    /// </summary>
    public string Password { get; } = password;

    /// <inheritdoc />
    public override string ToString() => $"UserName: {UserName}";

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as LoginInfo);

    /// <inheritdoc />
    public bool Equals(LoginInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(UserName, other.UserName, StringComparison.Ordinal) &&
               string.Equals(Password, other.Password, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(UserName, StringComparer.Ordinal);
        hash.Add(Password, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}
