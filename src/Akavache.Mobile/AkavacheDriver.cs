﻿// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ReactiveUI;

using Splat;

namespace Akavache.Mobile;

/// <summary>
/// The main driver for a mobile application. Used primary by the ReactiveUI project.
/// </summary>
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode("Registrations for Akavache.Mobile")]
[RequiresDynamicCode("Registrations for Akavache.Mobile")]
#endif
public class AkavacheDriver : ISuspensionDriver, IEnableLogger
{
    /// <inheritdoc />
    public IObservable<object> LoadState() => BlobCache.UserAccount.GetObject<object>("__AppState")!;

    /// <inheritdoc />
    public IObservable<Unit> SaveState(object state) =>
        BlobCache.UserAccount.InsertObject("__AppState", state)
            .SelectMany(BlobCache.UserAccount.Flush());

    /// <inheritdoc />
    public IObservable<Unit> InvalidateState() => BlobCache.UserAccount.InvalidateObject<object>("__AppState");
}
