// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// <para>
/// This interface indicates that the underlying BlobCache implementation
/// encrypts or otherwise secures its persisted content.
/// </para>
/// <para>
/// By implementing this interface, you must guarantee that the data
/// saved to disk cannot be easily read by a third party.
/// </para>
/// </summary>
public interface ISecureBlobCache : IBlobCache
{
}
