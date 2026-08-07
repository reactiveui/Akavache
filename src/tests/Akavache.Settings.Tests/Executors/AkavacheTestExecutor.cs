// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings.Tests.Executors;
#else
namespace Akavache.Settings.Tests.Executors;
#endif

/// <summary>
/// Standard test executor used by tests in this assembly. Delegates its reset
/// behaviour to <see cref="AkavacheTestExecutorBase"/> without any additional
/// configuration.
/// </summary>
public sealed class AkavacheTestExecutor : AkavacheTestExecutorBase;
