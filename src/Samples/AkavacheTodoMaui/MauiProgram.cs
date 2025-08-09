// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using AkavacheTodoMaui.Services;
using AkavacheTodoMaui.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Splat;

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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configure Akavache
        ConfigureAkavache();

        // Register services
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddTransient<MainViewModel>();

        // Configure ReactiveUI
        Locator.CurrentMutable.RegisterViewsForViewModels(typeof(MauiProgram).Assembly);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureAkavache()
    {
        // Initialize Akavache with System.Text.Json serializer for best performance
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        // Configure DateTime handling for consistent behavior
        CacheDatabase.ForcedDateTimeKind = DateTimeKind.Utc;

        // Initialize SQLite support - use the new V11 initialization pattern
        CacheDatabase.Initialize(builder =>
        {
            builder.WithApplicationName("AkavacheTodoMaui")
                   .WithSqliteDefaults();
        });
    }
}
