// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using AkavacheTodoMaui.Services;
using AkavacheTodoMaui.ViewModels;
using ReactiveUI;

namespace AkavacheTodoMaui;

/// <summary>
/// Main page demonstrating Akavache features with ReactiveUI MVVM.
/// </summary>
public partial class MainPage : ContentPage, IViewFor<MainViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();

        // Set up data context with dependency injection
        var notificationService = Handler?.MauiContext?.Services?.GetService<NotificationService>()
                                 ?? new NotificationService();

        ViewModel = new MainViewModel(notificationService);
        BindingContext = ViewModel;
    }

    /// <summary>
    /// Gets or sets the view model.
    /// </summary>
    public MainViewModel? ViewModel { get; set; }

    /// <summary>
    /// Gets or sets the view model as object.
    /// </summary>
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as MainViewModel;
    }

    /// <summary>
    /// Called when the page appears.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Activate the view model when page appears
        if (ViewModel is IActivatableViewModel activatable)
        {
            activatable.Activator.Activate();
        }
    }

    /// <summary>
    /// Called when the page disappears.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Deactivate the view model when page disappears
        if (ViewModel is IActivatableViewModel activatable)
        {
            activatable.Activator.Deactivate();
        }
    }
}
