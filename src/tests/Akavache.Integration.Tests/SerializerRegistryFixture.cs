// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Drives the process-wide serializer registry <see cref="UniversalSerializer"/> consults when the
/// primary serializer cannot read a payload. Shared by every fixture that needs a known set of
/// fallback serializers, or a clean registry, around a test.
/// </summary>
internal static class SerializerRegistryFixture
{
    /// <summary>Clears the registry so registered serializers do not bleed between tests.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Reset() => UniversalSerializer.ResetCaches();

    /// <summary>Registers every shipped serializer so the fallback paths have alternatives to try.</summary>
    internal static void RegisterAll()
    {
        UniversalSerializer.ResetCaches();
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonBsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new NewtonsoftSerializer());
        UniversalSerializer.RegisterSerializer(static () => new NewtonsoftBsonSerializer());
    }
}
