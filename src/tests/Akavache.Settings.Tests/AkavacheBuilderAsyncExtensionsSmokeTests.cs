// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;

using Splat.Builder;

namespace Akavache.Settings.Tests;

/// <summary>
/// Smoke coverage for <see cref="AkavacheBuilderAsyncExtensions"/>. Deep semantic
/// coverage lives in the observable-facing tests; these tests only verify that
/// the Task shims forward correctly so the <c>async</c>/<c>await</c> call pattern
/// continues to compile and execute for consumers that can't drop it.
/// </summary>
[Category("Akavache")]
public class AkavacheBuilderAsyncExtensionsSmokeTests
{
    /// <summary>
    /// Verifies <see cref="AkavacheBuilderAsyncExtensions.DisposeSettingsStoreAsync{T}"/>
    /// completes without throwing when no settings store has been registered for the
    /// type (the observable path also no-ops in this case).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeSettingsStoreAsyncShouldCompleteWhenNoneRegistered()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        IAkavacheInstance? instance = null;

        appBuilder.WithAkavache<SystemJsonSerializer>(
            $"dispose_smoke_{Guid.NewGuid():N}",
            _ => { },
            configured => instance = configured);

        await instance!.DisposeSettingsStoreAsync<ViewSettings>();
    }

    /// <summary>
    /// Verifies <see cref="AkavacheBuilderAsyncExtensions.DeleteSettingsStoreAsync{T}"/>
    /// completes without throwing when no settings store has been registered for the
    /// type. The delete shim's observable sibling swallows file-system errors for
    /// missing databases, so the smoke test just confirms the wrapper doesn't
    /// surface a different failure mode.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeleteSettingsStoreAsyncShouldCompleteWhenNoneRegistered()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        IAkavacheInstance? instance = null;

        appBuilder.WithAkavache<SystemJsonSerializer>(
            $"delete_smoke_{Guid.NewGuid():N}",
            _ => { },
            configured => instance = configured);

        await instance!.DeleteSettingsStoreAsync<ViewSettings>();
    }
}
