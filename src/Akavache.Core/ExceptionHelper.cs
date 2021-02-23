// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Akavache
{
    internal static class ExceptionHelper
    {
        public static IObservable<T> ObservableThrowKeyNotFoundException<T>(string key, Exception? innerException = null)
        {
            return Observable.Throw<T>(
                new KeyNotFoundException($"The given key '{key}' was not present in the cache.", innerException));
        }

        public static IObservable<T> ObservableThrowObjectDisposedException<T>(string obj, Exception? innerException = null)
        {
            return Observable.Throw<T>(
                new ObjectDisposedException($"The cache '{obj}' was disposed.", innerException));
        }
    }
}
