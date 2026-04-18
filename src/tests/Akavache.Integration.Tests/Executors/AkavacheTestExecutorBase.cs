// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Splat.Builder;
using TUnit.Core.Interfaces;

namespace Akavache.Tests.Executors;

/// <summary>
/// Test executor for the integration test assembly. Resets all Akavache global state
/// including both Sqlite3 and EncryptedSqlite3 provider flags.
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
    /// Runs in the <c>finally</c> block regardless of test outcome.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    protected virtual Task Clean() => ResetStateAsync();

    /// <summary>
    /// Resets every piece of Akavache static state.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    protected virtual Task ResetStateAsync()
    {
        CacheDatabase.ResetForTests().WaitForCompletion();
        RequestCache.Clear();
        UniversalSerializer.ResetCaches();
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
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
