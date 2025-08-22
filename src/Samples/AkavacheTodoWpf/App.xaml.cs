// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using System.Runtime.Versioning;
using System.Windows;
using Akavache;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using AkavacheTodoWpf.Services;
using AkavacheTodoWpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Splat;
using Splat.Builder;

namespace AkavacheTodoWpf;

/// <summary>
/// Interaction logic for App.xaml with Akavache and dependency injection setup.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
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
            // Step 1: Stop all ViewModels and their timers first
            if (_host != null)
            {
                var mainViewModel = _host.Services.GetService<MainViewModel>();
                if (mainViewModel != null)
                {
                    try
                    {
                        // Deactivate the view model to stop all timers and subscriptions
                        mainViewModel.Activator?.Deactivate();

                        // Save application state
                        await mainViewModel.SaveApplicationState().ToTask();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving application state: {ex}");
                    }
                }
            }

            // Step 2: Give a moment for any pending operations to complete
            await Task.Delay(500);

            // Step 3: Shutdown Akavache properly to flush all pending operations
            try
            {
                await CacheDatabase.Shutdown().ToTask();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during BlobCache shutdown: {ex}");
            }

            // Step 4: Shutdown dependency injection host
            if (_host != null)
            {
                try
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during host shutdown: {ex}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during application shutdown: {ex}");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    /// <summary>
    /// Use the builder pattern to configure Akavache with SQLite persistence.
    /// </summary>
    private static void ConfigureAkavache() =>
        AppBuilder.CreateSplatBuilder()
            .WithAkavache(builder =>
                builder.UseForcedDateTimeKind(DateTimeKind.Utc)
                        .UseSystemTextJson()
                        .WithApplicationName("AkavacheTodoWpf")
                        .WithSqliteDefaults());

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
        mainWindow.Loaded += (s, e) => mainViewModel.Activator?.Activate();
        mainViewModel.Activator.Activate();
    }
}
