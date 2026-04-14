// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache;
using AkavacheTodoMaui.Services;

namespace AkavacheTodoMaui;

/// <summary>
/// Main application class with Akavache lifecycle management.
/// </summary>
[RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
[RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
public partial class App : Application
{
    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
#pragma warning disable IL3050 // AOT reflection warnings are expected for ReactiveUI in sample applications
    public App()
    {
        InitializeComponent();
    }
#pragma warning restore IL3050

    /// <summary>
    /// Creates the main application window.
    /// </summary>
    /// <param name="activationState">The activation state.</param>
    /// <returns>The main window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = new(new AppShell());

        // Handle window lifecycle events
        window.Created += OnWindowCreated;
        window.Destroying += OnWindowDestroying;

        return window;
    }

    /// <summary>
    /// Called when the application starts.
    /// </summary>
    protected override void OnStart()
    {
        base.OnStart();
        System.Diagnostics.Debug.WriteLine("Application started");
    }

    /// <summary>
    /// Called when the application is put to sleep.
    /// </summary>
    protected override void OnSleep()
    {
        base.OnSleep();

        // Save application state when going to sleep
        TodoCacheService.SaveApplicationState()
            .Subscribe(
                static _ => System.Diagnostics.Debug.WriteLine("Application state saved"),
                static ex => System.Diagnostics.Debug.WriteLine($"Failed to save state: {ex}"));
    }

    /// <summary>
    /// Called when the application resumes.
    /// </summary>
    protected override void OnResume()
    {
        base.OnResume();

        // Optionally refresh data when resuming
        System.Diagnostics.Debug.WriteLine("Application resumed");
    }

    /// <summary>
    /// Called when a window is created.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event data.</param>
    private void OnWindowCreated(object? sender, EventArgs e) => System.Diagnostics.Debug.WriteLine("Window created");

    /// <summary>
    /// Called when a window is being destroyed to save application state and shut down the cache.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event data.</param>
    private async void OnWindowDestroying(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Window destroying - saving state and shutting down cache");

        try
        {
            // Save final application state
            await TodoCacheService.SaveApplicationState();

            // Shutdown Akavache properly to ensure all data is flushed
            await CacheDatabase.Shutdown();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex}");
        }
    }
}
