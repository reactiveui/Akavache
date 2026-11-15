// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Helpers;
#else
namespace Akavache.Helpers;
#endif

/// <summary>Internal extension members that classify <see cref="Type"/> instances for DateTime-aware serialization.</summary>
internal static class DateTimeTypeExtensions
{
    /// <summary>Extension members for <see cref="Type"/>.</summary>
    /// <param name="type">The type to test.</param>
    extension(Type type)
    {
        /// <summary>Determines whether <paramref name="type"/> is <see cref="DateTime"/> or its nullable form.</summary>
        /// <returns><c>true</c> when the type needs the DateTime-aware serialization shim.</returns>
        internal bool IsDateTime() => type == typeof(DateTime) || type == typeof(DateTime?);

        /// <summary>Determines whether <paramref name="type"/> is <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, or either of their nullable forms.</summary>
        /// <returns><c>true</c> when the type can be recovered by the DateTime-aware deserialization fallback.</returns>
        internal bool IsDateTimeOrDateTimeOffset() =>
            type.IsDateTime() || type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);
    }
}
