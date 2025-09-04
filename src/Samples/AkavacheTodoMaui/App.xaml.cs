// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    public App() => InitializeComponent();

    /// <summary>
    /// Creates the main application window.
    /// </summary>
    /// <param name="activationState">The activation state.</param>
    /// <returns>The main window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

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

    private void OnWindowCreated(object? sender, EventArgs e) => System.Diagnostics.Debug.WriteLine("Window created");

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
