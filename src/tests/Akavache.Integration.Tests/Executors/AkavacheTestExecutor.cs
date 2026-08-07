// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Executors;
#else
namespace Akavache.Tests.Executors;
#endif

/// <summary>Standard test executor used by tests in this assembly.</summary>
public sealed class AkavacheTestExecutor : AkavacheTestExecutorBase;
