// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace System.IO.IsolatedStorage;

/// <summary>
/// An exception that happens when there is a isolated storage issue.
/// </summary>
public class IsolatedStorageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsolatedStorageException"/> class.
    /// </summary>
    /// <param name="message">The message about the exception.</param>
    public IsolatedStorageException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IsolatedStorageException"/> class.
    /// </summary>
    /// <param name="message">The message about the exception.</param>
    /// <param name="innerException">An inner exception with further details.</param>
    public IsolatedStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}