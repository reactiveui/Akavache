// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace System
{
    /// <summary>
    /// A set of extension methods associated with the <see cref="Stream"/> class.
    /// </summary>
    public static class StreamMixins
    {
        /// <summary>
        /// Writes to a stream and returns a observable.
        /// </summary>
        /// <param name="blobCache">The stream to write to.</param>
        /// <param name="data">The data to write.</param>
        /// <param name="start">The start location where to write from.</param>
        /// <param name="length">The length in bytes to read.</param>
        /// <returns>An observable that signals when the write operation has completed.</returns>
        public static IObservable<Unit> WriteAsyncRx(this Stream blobCache, byte[] data, int start, int length)
        {
#if NETSTANDARD || XAMARINIOS || XAMARINMAC || XAMARINTVOS || TIZEN || MONOANDROID13_0
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }
#else
            ArgumentNullException.ThrowIfNull(blobCache);
#endif

            var ret = new AsyncSubject<Unit>();

            try
            {
                blobCache.BeginWrite(
                    data,
                    start,
                    length,
                    result =>
                    {
                        try
                        {
                            blobCache.EndWrite(result);
                            ret.OnNext(Unit.Default);
                            ret.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            ret.OnError(ex);
                        }
                    },
                    null);
            }
            catch (Exception ex)
            {
                ret.OnError(ex);
            }

            return ret;
        }
    }
}
