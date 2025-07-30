// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using AkavacheTodoWpf.Models;
using AkavacheTodoWpf.Services;
using ReactiveUI;

namespace AkavacheTodoWpf.ViewModels;

/// <summary>
/// Simple edit dialog for demo purposes.
/// </summary>
internal class EditTodoDialog : Window
{
    private readonly System.Windows.Controls.TextBox _textBox;

    public EditTodoDialog(string currentTitle)
    {
        Title = "Edit Todo";
        Width = 400;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        _textBox = new System.Windows.Controls.TextBox
        {
            Text = currentTitle,
            Margin = new Thickness(10),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14
        };
        System.Windows.Controls.Grid.SetRow(_textBox, 0);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10)
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 75,
            Height = 30,
            Margin = new Thickness(5, 0, 0, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) =>
        {
            DialogResult = true;
            Close();
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            Width = 75,
            Height = 30,
            Margin = new Thickness(5, 0, 0, 0),
            IsCancel = true
        };
        cancelButton.Click += (s, e) =>
        {
            DialogResult = false;
            Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 1);

        grid.Children.Add(_textBox);
        grid.Children.Add(buttonPanel);

        Content = grid;

        Loaded += (s, e) => _textBox.Focus();
    }

    public string TodoTitle => _textBox.Text;
}
