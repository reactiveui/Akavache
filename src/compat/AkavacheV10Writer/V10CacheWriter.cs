// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;

namespace AkavacheV10Writer;

/// <summary>
/// Writes <see cref="CompatDataset"/> into an Akavache 10 database. Reporting is a callback rather
/// than a direct write so the entry point owns where the progress goes.
/// </summary>
/// <param name="cache">The V10 cache to write into.</param>
/// <param name="report">Receives one line per entry written.</param>
internal sealed class V10CacheWriter(SqlRawPersistentBlobCache cache, Action<string> report)
{
    /// <summary>Writes every entry in the dataset and flushes the cache.</summary>
    internal void WriteDataset()
    {
        var person = CompatDataset.PersonValue;
        var bytes = CompatDataset.CreateBytesValue();

        _ = cache.InsertObject(CompatDataset.StringKey, CompatDataset.StringValue).Wait();
        report($"Inserted: key='{CompatDataset.StringKey}', type=string, value='{CompatDataset.StringValue}'");

        _ = cache.InsertObject(CompatDataset.IntKey, CompatDataset.IntValue).Wait();
        report($"Inserted: key='{CompatDataset.IntKey}', type=int, value={CompatDataset.IntValue}");

        _ = cache.InsertObject(CompatDataset.PersonKey, person).Wait();
        report($"Inserted: key='{CompatDataset.PersonKey}', type=Person, value={{Name={person.Name},Age={person.Age},Email={person.Email}}}");

        _ = cache.Insert(CompatDataset.BytesKey, bytes).Wait();
        report($"Inserted: key='{CompatDataset.BytesKey}', type=byte[], value='{BitConverter.ToString(bytes)}'");

        _ = cache.Flush().Wait();
    }
}
