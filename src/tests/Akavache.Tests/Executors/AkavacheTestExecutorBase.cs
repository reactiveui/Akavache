// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Splat;
using Splat.Builder;
using TUnit.Core.Interfaces;

namespace Akavache.Tests.Executors;

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

        await ResetStateAsync().ConfigureAwait(false);
        ConfigureBuilder();

        try
        {
            await testAction().ConfigureAwait(false);
        }
        finally
        {
            await ResetStateAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resets every piece of Akavache and Splat static state. Override to reset
    /// additional state holders owned by a particular test class.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    protected virtual async Task ResetStateAsync()
    {
        await CacheDatabase.ResetForTestsAsync().ConfigureAwait(false);

        RequestCache.Clear();

        AkavacheBuilder.SettingsStores = [];
        AkavacheBuilder.BlobCaches = [];

        var previousLocator = AppLocator.GetLocator();
        var freshLocator = new ModernDependencyResolver();
        AppLocator.SetLocator(freshLocator);
        AppLocator.CurrentMutable.InitializeSplat();

        if (!ReferenceEquals(previousLocator, freshLocator))
        {
            previousLocator?.Dispose();
        }

        AppBuilder.ResetBuilderStateForTests();
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
