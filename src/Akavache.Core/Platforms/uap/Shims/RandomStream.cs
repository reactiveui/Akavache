// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO;

/// <summary>
/// A implementation of the random access stream.
/// </summary>
public sealed class RandomStream : IRandomAccessStream
{
    private readonly Stream _streamValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomStream"/> class.
    /// </summary>
    /// <param name="stream">The stream to wrap.</param>
    public RandomStream(Stream stream) => _streamValue = stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomStream"/> class.
    /// </summary>
    /// <param name="bytes">The byte array to wrap.</param>
    public RandomStream(byte[] bytes) => _streamValue = new MemoryStream(bytes);

    /// <inheritdoc />
    public ulong Size
    {
        get => (ulong)_streamValue.Length;

        set => _streamValue.SetLength((long)value);
    }

    /// <inheritdoc />
    public bool CanRead => true;

    /// <inheritdoc />
    public bool CanWrite => true;

    /// <inheritdoc />
    public ulong Position => (ulong)_streamValue.Position;

    /// <inheritdoc />
    public IInputStream GetInputStreamAt(ulong position)
    {
        if ((long)position > _streamValue.Length)
        {
            throw new IndexOutOfRangeException();
        }

        _streamValue.Position = (long)position;

        return _streamValue.AsInputStream();
    }

    /// <inheritdoc />
    public IOutputStream GetOutputStreamAt(ulong position)
    {
        if ((long)position > _streamValue.Length)
        {
            throw new IndexOutOfRangeException();
        }

        _streamValue.Position = (long)position;

        return _streamValue.AsOutputStream();
    }

    /// <inheritdoc />
    public IRandomAccessStream CloneStream() => throw new NotSupportedException();

    /// <inheritdoc />
    public void Seek(ulong position) => _streamValue.Seek((long)position, 0);

    /// <inheritdoc />
    public void Dispose() => _streamValue.Dispose();

    /// <inheritdoc />
    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) => throw new NotSupportedException();

    /// <inheritdoc />
    public IAsyncOperation<bool> FlushAsync() => throw new NotImplementedException();

    /// <inheritdoc />
    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => throw new NotImplementedException();
}