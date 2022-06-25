// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Windows.Storage.Streams;

namespace System.IO;

/// <summary>
/// A set of extension methods associated with streams.
/// </summary>
public static class MicrosoftStreamExtensions
{
    /// <summary>
    /// Gets a random access stream from a stream.
    /// </summary>
    /// <param name="stream">The stream to convert.</param>
    /// <returns>The random access stream.</returns>
    public static IRandomAccessStream AsRandomAccessStream(this Stream stream) => new RandomStream(stream);

    /// <summary>
    /// Converts a byte array into a random access stream.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>The random access stream.</returns>
    public static IRandomAccessStream AsRandomAccessStream(this byte[] bytes) => new RandomStream(bytes);
}