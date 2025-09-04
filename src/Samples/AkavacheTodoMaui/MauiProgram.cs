// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using AkavacheTodoMaui.Services;
using AkavacheTodoMaui.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Splat;
using Splat.Builder;

namespace AkavacheTodoMaui;

/// <summary>
/// MAUI program startup configuration.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application.
    /// </summary>
    /// <returns>The configured MAUI app.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(static fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddTransient<MainViewModel>();

        // Configure Akavache
        ConfigureAkavache();

        // Configure ReactiveUI
        Locator.CurrentMutable.RegisterViewsForViewModels(typeof(MauiProgram).Assembly);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Initialize SQLite support - use the new V11 initialization pattern.
    /// </summary>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    private static IAppBuilder ConfigureAkavache() =>
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(static builder =>
                builder.WithApplicationName("AkavacheTodoMaui")
                    .UseForcedDateTimeKind(DateTimeKind.Utc)
                    .WithSqliteProvider()
                    .WithSqliteDefaults());
}
