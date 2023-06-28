// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache
{
    /// <summary>
    /// Provides encryption for blob caching.
    /// </summary>
    public class EncryptionProvider : IEncryptionProvider
    {
        /// <inheritdoc />
        public IObservable<byte[]> EncryptBlock(byte[] block) =>
            Observable.Return(
                ProtectedData.Protect(block, null, DataProtectionScope.CurrentUser));

        /// <inheritdoc />
        public IObservable<byte[]> DecryptBlock(byte[] block) => Observable.Return(ProtectedData.Unprotect(block, null, DataProtectionScope.CurrentUser));
    }
}
