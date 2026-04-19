// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Tests;
using Splat.Builder;
using TUnit.Core.Interfaces;

namespace Akavache.Settings.Tests.Executors;

/// <summary>
/// Base test executor that resets every piece of Akavache global state before and
/// after each test, then gives subclasses a hook to bootstrap any per-test-class
/// configuration. Follows the ReactiveUI <c>BuilderTestExecutorBase</c> pattern.
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
    /// Runs in the <c>finally</c> block of <see cref="ExecuteTest"/>, regardless
    /// of whether the test action succeeded or threw. Defaults to a full
    /// <see cref="ResetStateAsync"/>; override when a test class needs to dispose
    /// additional per-test fixtures before the shared state is torn down.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    protected virtual Task Clean() => ResetStateAsync();

    /// <summary>
    /// Resets every piece of Akavache static state. The Splat <c>AppLocator</c>
    /// instance is intentionally <em>not</em> replaced here: per-class
    /// <c>[Before(Test)]</c> hooks register Splat services before TUnit invokes
    /// our executor, so swapping the locator out from under them would wipe their
    /// fixture setup. Akavache's own builder registrations are idempotent, so
    /// leaving the locator intact does not leak serializer state across tests.
    /// Override to reset additional state holders owned by a particular test class.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    protected virtual Task ResetStateAsync()
    {
        CacheDatabase.ResetForTests().WaitForCompletion();

        RequestCache.Clear();

        // Clear UniversalSerializer's registered-factory list and cached alternatives
        // so a fake serializer registered by an earlier test cannot leak into the
        // next one.
        UniversalSerializer.ResetCaches();

        // Clear the cached Sqlite3 / EncryptedSqlite3 provider flags so the next
        // test's WithSqliteDefaults() / WithEncryptedSqliteDefaults() re-triggers
        // provider bootstrap.
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();

        AppBuilder.ResetBuilderStateForTests();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Bootstraps per-test-class configuration after <see cref="ResetStateAsync"/>
    /// has run. The default is a no-op; override when a test class needs a fresh
    /// builder seeded with a particular serializer or registrations.
    /// </summary>
    protected virtual void ConfigureBuilder()
    {
    }
}
