// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Provides the ability to encrypt and decrypt byte blocks.
/// </summary>
public interface IEncryptionProvider
{
    /// <summary>
    /// Encrypts a specified block.
    /// </summary>
    /// <param name="block">The block to encrypt.</param>
    /// <returns>An observable with the encrypted value.</returns>
    IObservable<byte[]> EncryptBlock(byte[] block);

    /// <summary>
    /// Decrypts a specified block.
    /// </summary>
    /// <param name="block">The block to decrypt.</param>
    /// <returns>An observable with the decrypted value.</returns>
    IObservable<byte[]> DecryptBlock(byte[] block);
}