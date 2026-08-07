// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings;
#else
namespace Akavache.Settings;
#endif

/// <summary>Supplies the state-passing <c>GetOrAdd</c> shape that .NET Framework does not carry.</summary>
internal static class ConcurrentDictionaryExtensions
{
    /// <summary>Extension members for <c>ConcurrentDictionary&lt;TKey, TValue&gt;</c>.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary to read or extend.</param>
    extension<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        /// <summary>
        /// Returns the existing value for <paramref name="key"/>, or adds the one produced by
        /// <paramref name="valueFactory"/>. The factory receives <paramref name="factoryArgument"/>
        /// rather than capturing it, so callers can pass a <see langword="static"/> lambda and avoid
        /// allocating a closure on every call.
        /// </summary>
        /// <typeparam name="TArg">The type of the state handed to the factory.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="valueFactory">Produces the value when the key is absent.</param>
        /// <param name="factoryArgument">State handed to <paramref name="valueFactory"/>.</param>
        /// <returns>The value already stored under <paramref name="key"/>, or the newly added one.</returns>
        /// <remarks>
        /// As with the framework overload, a race can invoke <paramref name="valueFactory"/> more than
        /// once; only one result is stored, and that result is what every caller sees.
        /// </remarks>
        internal TValue GetOrAddWithState<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            ArgumentExceptionHelper.ThrowIfNull(dictionary);
            ArgumentExceptionHelper.ThrowIfNull(valueFactory);

#if NETFRAMEWORK
            // .NET Framework has no state-passing overload. Probing first keeps the common hit
            // path off the factory entirely; the add path then uses the value-taking overload,
            // which needs no delegate and so captures nothing.
            return dictionary.TryGetValue(key, out var existing)
                ? existing
                : dictionary.GetOrAdd(key, valueFactory(key, factoryArgument));
#else
            return dictionary.GetOrAdd(key, valueFactory, factoryArgument);
#endif
        }
    }
}
