// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// A shim to allow the use of protected data.
/// </summary>
public static class ProtectedData
{
    /// <summary>
    /// Protected the specified data.
    /// </summary>
    /// <param name="originalData">The original data being passed.</param>
    /// <param name="entropy">Entropy to help with randomness.</param>
    /// <param name="scope">The scope where to store the data.</param>
    /// <returns>The original data.</returns>
    public static byte[] Protect(byte[] originalData, byte[]? entropy, DataProtectionScope scope = DataProtectionScope.CurrentUser) => originalData;

    /// <summary>
    /// Unprotected the specified data.
    /// </summary>
    /// <param name="originalData">The original data being passed.</param>
    /// <param name="entropy">Entropy to help with randomness.</param>
    /// <param name="scope">The scope where to store the data.</param>
    /// <returns>The original data.</returns>
    public static byte[] Unprotect(byte[] originalData, byte[]? entropy, DataProtectionScope scope = DataProtectionScope.CurrentUser) => originalData;
}
