// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows;
using AkavacheTodoWpf.ViewModels;
using ReactiveUI;

namespace AkavacheTodoWpf;

/// <summary>
/// Interaction logic for MainWindow.xaml with ReactiveUI integration.
/// </summary>
public partial class MainWindow : Window, IViewFor<MainViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Setup window state persistence
        LoadWindowState();
    }

    /// <summary>
    /// Gets or sets the view model.
    /// </summary>
    public MainViewModel? ViewModel
    {
        get => DataContext as MainViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Gets or sets the view model as object.
    /// </summary>
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as MainViewModel;
    }

    /// <summary>
    /// Called when the window is loaded.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Activate the view model when window loads
        if (ViewModel is IActivatableViewModel activatable)
        {
            activatable.Activator.Activate();
        }
    }

    /// <summary>
    /// Called when the window is closing.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save window state
        SaveWindowState();

        // Save application state
        if (ViewModel != null)
        {
            try
            {
                await ViewModel.SaveApplicationState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving application state: {ex}");
            }
        }

        // Deactivate the view model
        if (ViewModel is IActivatableViewModel activatable)
        {
            activatable.Activator.Deactivate();
        }
    }

    private void LoadWindowState()
    {
        if (ViewModel?.Settings != null)
        {
            var settings = ViewModel.Settings;

            if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }

            if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
            {
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }

            // Ensure window is visible on screen
            EnsureWindowIsVisible();
        }
    }

    private void SaveWindowState()
    {
        if (ViewModel?.Settings != null)
        {
            var settings = ViewModel.Settings;

            if (WindowState == WindowState.Normal)
            {
                settings.WindowWidth = Width;
                settings.WindowHeight = Height;
                settings.WindowLeft = Left;
                settings.WindowTop = Top;
            }
        }
    }

    private void EnsureWindowIsVisible()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        if (Left < 0 || Left > screenWidth - Width)
        {
            Left = (screenWidth - Width) / 2;
        }

        if (Top < 0 || Top > screenHeight - Height)
        {
            Top = (screenHeight - Height) / 2;
        }
    }
}
