// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache;
using Akavache.Sqlite3;

namespace AkavacheV11Reader;

/// <summary>
/// Reads every entry of <see cref="CompatDataset"/> back out of a database Akavache 10 wrote, and
/// reports whether each still round-trips. A read that throws is a failed entry rather than a failed
/// run, so one broken type does not hide the state of the others.
/// </summary>
/// <param name="cache">The V11 cache opened over the V10 database file.</param>
internal sealed class V11CacheVerifier(SqliteBlobCache cache)
{
    /// <summary>Verifies every entry in the dataset.</summary>
    /// <returns>One result per entry, in the order the writer stored them.</returns>
    internal async Task<IReadOnlyList<VerificationResult>> VerifyDatasetAsync()
    {
        var person = CompatDataset.PersonValue;

        return
        [
            await VerifyAsync(
                CompatDataset.StringKey,
                "string",
                async () => await cache.GetObject<string>(CompatDataset.StringKey),
                static value => value == CompatDataset.StringValue,
                static value => $"got '{value}'"),
            await VerifyAsync(
                CompatDataset.IntKey,
                "int",
                async () => await cache.GetObject<int>(CompatDataset.IntKey),
                static value => value == CompatDataset.IntValue,
                static value => $"got {value}"),
            await VerifyAsync(
                CompatDataset.PersonKey,
                nameof(Person),
                async () => await cache.GetObject<Person>(CompatDataset.PersonKey),
                value => value?.Name == person.Name && value?.Age == person.Age && value?.Email == person.Email,
                static value => $"got Name={value?.Name},Age={value?.Age},Email={value?.Email}"),
            await VerifyAsync(
                CompatDataset.BytesKey,
                "byte[]",
                async () => await cache.Get(CompatDataset.BytesKey),
                static value => value?.SequenceEqual(CompatDataset.CreateBytesValue()) == true,
                static value => $"len={value?.Length}"),
        ];
    }

    /// <summary>Reads one entry and turns the outcome into a result.</summary>
    /// <typeparam name="T">The type the entry is read as.</typeparam>
    /// <param name="key">The cache key to read.</param>
    /// <param name="typeName">The type name to report.</param>
    /// <param name="read">Reads the value.</param>
    /// <param name="matches">Decides whether the value is the one the writer stored.</param>
    /// <param name="describe">Describes the value for a failure line.</param>
    /// <returns>The result for this entry.</returns>
    private static async Task<VerificationResult> VerifyAsync<T>(
        string key,
        string typeName,
        Func<Task<T?>> read,
        Func<T?, bool> matches,
        Func<T?, string> describe)
    {
        try
        {
            var value = await read();
            return matches(value)
                ? new(key, typeName, Passed: true, Detail: null)
                : new(key, typeName, Passed: false, describe(value));
        }
        catch (Exception ex)
        {
            return new(key, typeName, Passed: false, $"threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
