// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// The scope in which to store data stored by <see cref="ProtectedData" />.
/// </summary>
public enum DataProtectionScope
{
    /// <summary>
    /// Store the data underneath the current user.
    /// </summary>
    CurrentUser,
}