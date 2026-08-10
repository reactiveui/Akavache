// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>
/// This interface indicates that the underlying BlobCache implementation
/// encrypts or otherwise secures its persisted content. By implementing this
/// interface, you must guarantee that the data saved to disk cannot be easily
/// read by a third party.
/// </summary>
public interface ISecureBlobCache : IBlobCache;
