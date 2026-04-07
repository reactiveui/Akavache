// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Splat;
using Splat.Builder;
using TUnit.Core.Interfaces;

namespace Akavache.Settings.Tests.Executors;

/// <summary>
/// Test executor that resets all Akavache static state before and after each test.
/// This ensures test isolation when tests share global state such as
/// <see cref="Akavache.Core.AkavacheBuilder.SettingsStores"/>,
/// <see cref="Akavache.Core.AkavacheBuilder.BlobCaches"/>, and <see cref="CacheDatabase"/>.
/// </summary>
public class AkavacheTestExecutor : ITestExecutor
{
    /// <inheritdoc />
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await ResetStateAsync().ConfigureAwait(false);

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            await ResetStateAsync().ConfigureAwait(false);
        }
    }

    private static async Task ResetStateAsync()
    {
        // Reset CacheDatabase completely (async shutdown + clear static state)
        await CacheDatabase.ResetForTestsAsync().ConfigureAwait(false);

        // Reset Akavache builder static state (settings stores and blob caches)
        AkavacheBuilder.SettingsStores = [];
        AkavacheBuilder.BlobCaches = [];

        // Clear registered serializers to avoid cross-test pollution
        try
        {
            if (AppLocator.CurrentMutable.HasRegistration(typeof(ISerializer)))
            {
                AppLocator.CurrentMutable.UnregisterAll(typeof(ISerializer));
            }
        }
        catch (Exception ex)
        {
            // Best-effort: serializer cleanup may fail if no container is configured
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }

        AppBuilder.ResetBuilderStateForTests();
    }
}
