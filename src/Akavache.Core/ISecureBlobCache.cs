// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// This interface indicates that the underlying BlobCache implementation
/// encrypts or otherwise secures its persisted content.
///
/// By implementing this interface, you must guarantee that the data
/// saved to disk cannot be easily read by a third party.
/// </summary>
public interface ISecureBlobCache : IBlobCache;
