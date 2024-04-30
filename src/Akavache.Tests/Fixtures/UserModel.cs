// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// A mock for the user models.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UserModel"/> class.
/// </remarks>
/// <param name="user">The user to abstract.</param>
public class UserModel(UserObject user)
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the age.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the user.
    /// </summary>
    public UserObject User { get; set; } = user;
}