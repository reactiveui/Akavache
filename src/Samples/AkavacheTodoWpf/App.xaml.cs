// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows;
using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using AkavacheTodoWpf.Services;
using AkavacheTodoWpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AkavacheTodoWpf;

/// <summary>
/// Interaction logic for App.xaml with Akavache and dependency injection setup.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Called when the application starts.
    /// </summary>
    /// <param name="e">The startup event args.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Akavache using the new builder pattern
        ConfigureAkavache();

        // Setup dependency injection
        _host = CreateHostBuilder().Build();

        // Start the application
        StartApplication();
    }

    /// <summary>
    /// Called when the application shuts down.
    /// </summary>
    /// <param name="e">The exit event args.</param>
    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            // Shutdown Akavache properly to flush all pending operations
            await BlobCache.Shutdown();

            // Shutdown dependency injection host
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex}");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private static void ConfigureAkavache()
    {
        // Step 1: Initialize the serializer first
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        // Step 2: Configure DateTime handling for consistent behavior
        BlobCache.ForcedDateTimeKind = DateTimeKind.Utc;

        ////// Step 3: Initialize SQLite support
        ////SQLitePCL.Batteries_V2.Init();

        // Step 4: Use the builder pattern to configure Akavache with SQLite persistence
        BlobCache.Initialize(builder =>
        {
            builder.WithApplicationName("AkavacheTodoWpf")
                   .WithSqliteDefaults(); // This creates SQLite caches for all cache types
        });
    }

    private static IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services for dependency injection
                services.AddSingleton<NotificationService>();

                // Register view models
                services.AddTransient<MainViewModel>();
            });

    private void StartApplication()
    {
        // Create and show main window with dependency injection
        var notificationService = _host!.Services.GetRequiredService<NotificationService>();
        var mainViewModel = new MainViewModel(notificationService);

        var mainWindow = new MainWindow
        {
            ViewModel = mainViewModel
        };

        mainWindow.Show();

        // Force activation after window is shown
        mainWindow.Loaded += (s, e) =>
        {
            mainViewModel.Activator?.Activate();
        };
        mainViewModel.Activator.Activate();
    }
}
