// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Stored login information for a user.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoginInfo"/> class.
/// </remarks>
/// <param name="username">The username for the entry.</param>
/// <param name="password">The password for the user.</param>
public class LoginInfo(string username, string password)
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
}