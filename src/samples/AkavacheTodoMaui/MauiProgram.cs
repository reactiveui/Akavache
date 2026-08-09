// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Akavache;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using AkavacheTodoMaui.Services;
using AkavacheTodoMaui.ViewModels;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif
using ReactiveUI;
using ReactiveUI.Builder;
using Splat;
using Splat.Builder;

namespace AkavacheTodoMaui;

/// <summary>MAUI program startup configuration.</summary>
public static class MauiProgram
{
    /// <summary>Creates and configures the MAUI application.</summary>
    /// <returns>The configured MAUI app.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        _ = builder
            .UseMauiApp<App>()
            .ConfigureFonts(static fonts =>
            {
                _ = fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                _ = fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        _ = builder.Services.AddSingleton<NotificationService>();
        _ = builder.Services.AddTransient<MainViewModel>();

        // Configure Akavache
        _ = ConfigureAkavache();

        // Configure ReactiveUI
        Locator.CurrentMutable.RegisterViewsForViewModels(typeof(MauiProgram).Assembly);

#if DEBUG
        _ = builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>Initialize SQLite support - use the new V11 initialization pattern.</summary>
    /// <returns>The application builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    private static IAppBuilder ConfigureAkavache() =>
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithMaui()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(
                static builder => builder
                    .UseForcedDateTimeKind(DateTimeKind.Utc)
                    .WithSqliteProvider()
                    .WithSqliteDefaults(),
                "AkavacheTodoMaui")
            .BuildApp();
}
