// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Security.Cryptography.DataProtection;

namespace Akavache;

/// <summary>
/// A encryption provider for the WinRT system.
/// </summary>
public class WinRTEncryptionProvider : IEncryptionProvider
{
    /// <inheritdoc />
    public IObservable<byte[]> EncryptBlock(byte[] block)
    {
        var dpapi = new DataProtectionProvider("LOCAL=user");
        return dpapi.ProtectAsync(block.AsBuffer()).ToObservable().Select(b => b.ToArray());
    }

    /// <inheritdoc />
    public IObservable<byte[]> DecryptBlock(byte[] block)
    {
        // Do not include a protectionDescriptor
        // http://msdn.microsoft.com/en-us/library/windows/apps/windows.security.cryptography.dataprotection.dataprotectionprovider.unprotectasync.aspx
        var dpapi = new DataProtectionProvider();
        return dpapi.UnprotectAsync(block.AsBuffer()).ToObservable().Select(b => b.ToArray());
    }
}