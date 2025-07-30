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
using ReactiveUI;
using Splat;

namespace AkavacheTodoWpf;

/// <summary>
/// Interaction logic for App.xaml with Akavache and dependency injection setup.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private TodoCacheService? _cacheService;

    /// <summary>
    /// Called when the application starts.
    /// </summary>
    /// <param name="e">The startup event args.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Akavache
        ConfigureAkavache();

        // Setup dependency injection
        _host = CreateHostBuilder().Build();

        // Configure ReactiveUI
        ConfigureReactiveUI();

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
            // Save application state
            if (_cacheService != null)
            {
                await TodoCacheService.SaveApplicationState();
            }

            // Shutdown Akavache properly
            await BlobCache.Shutdown();

            // Shutdown host
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
        // Initialize Akavache with System.Text.Json serializer for best performance
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        // Initialize SQLite support - use the new V11 initialization pattern
        BlobCache.Initialize(builder =>
        {
            builder.WithApplicationName("AkavacheTodoWpf")
                   .WithSqliteDefaults();
        });

        // Configure DateTime handling for consistent behavior
        BlobCache.ForcedDateTimeKind = DateTimeKind.Utc;

        // Initialize SQLite
        SQLitePCL.Batteries_V2.Init();
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<TodoCacheService>();
                services.AddSingleton<NotificationService>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>();
            });
    }

    private static void ConfigureReactiveUI()
    {
        // Register views for view models
        Locator.CurrentMutable.Register<IViewFor<MainViewModel>>(() => new MainWindow());
    }

    private void StartApplication()
    {
        // Get services
        _cacheService = _host!.Services.GetRequiredService<TodoCacheService>();
        var notificationService = _host.Services.GetRequiredService<NotificationService>();

        // Create and show main window
        var mainViewModel = new MainViewModel(_cacheService, notificationService);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
