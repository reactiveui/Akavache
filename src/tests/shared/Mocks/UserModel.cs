// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Mocks;
#else
namespace Akavache.Tests.Mocks;
#endif

/// <summary>A mock for the user models.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UserModel"/> class.
/// </remarks>
/// <param name="user">The user to abstract.</param>
[System.Diagnostics.DebuggerDisplay("{Name}")]
public class UserModel(UserObject user)
{
    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the age.</summary>
    public int Age { get; set; }

    /// <summary>Gets or sets the user.</summary>
    public UserObject User { get; set; } = user;
}
