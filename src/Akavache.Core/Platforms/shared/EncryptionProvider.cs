// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;

#if NET_461
using RxCrypt = System.Security.Cryptography;
#else
using RxCrypt = Akavache;
#endif

namespace Akavache
{
    /// <summary>
    /// Provides encryption for blob caching.
    /// </summary>
    public class EncryptionProvider : IEncryptionProvider
    {
        /// <inheritdoc />
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            return Observable.Return(
                RxCrypt.ProtectedData.Protect(block, null, RxCrypt.DataProtectionScope.CurrentUser));
        }

        /// <inheritdoc />
        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            return Observable.Return(RxCrypt.ProtectedData.Unprotect(block, null, RxCrypt.DataProtectionScope.CurrentUser));
        }
    }
}
