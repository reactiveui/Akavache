// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Splat.Builder;
using TUnit.Core.Interfaces;

namespace Akavache.Tests.Executors;

/// <summary>
/// Core-only test executor that resets Akavache global state before and after
/// each test without touching Sqlite3 or EncryptedSqlite3 provider flags.
/// </summary>
public class AkavacheTestExecutorBase : ITestExecutor
{
    /// <inheritdoc />
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> testAction)
    {
        ArgumentNullException.ThrowIfNull(testAction);

        try
        {
            await ResetStateAsync().ConfigureAwait(false);
            ConfigureBuilder();
            await testAction().ConfigureAwait(false);
        }
        finally
        {
            await Clean().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs in the <c>finally</c> block of <see cref="ExecuteTest"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    protected virtual Task Clean() => ResetStateAsync();

    /// <summary>
    /// Resets Akavache Core static state. Does not touch Sqlite3 or EncryptedSqlite3
    /// provider flags since this assembly has no dependency on those packages.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    protected virtual Task ResetStateAsync()
    {
        CacheDatabase.ResetForTests().WaitForCompletion();

        RequestCache.Clear();
        UniversalSerializer.ResetCaches();
        AppBuilder.ResetBuilderStateForTests();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Bootstraps per-test-class configuration after state reset.
    /// </summary>
    protected virtual void ConfigureBuilder()
    {
    }
}
