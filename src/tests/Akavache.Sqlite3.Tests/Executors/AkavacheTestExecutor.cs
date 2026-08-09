// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Executors;
#else
namespace Akavache.Tests.Executors;
#endif

/// <summary>Standard test executor used by tests in this assembly.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed class AkavacheTestExecutor : AkavacheTestExecutorBase;
