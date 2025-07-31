// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows;
using AkavacheTodoWpf.Models;

namespace AkavacheTodoWpf.Views;

/// <summary>
/// EditTodoDialog for comprehensive todo editing.
/// </summary>
public partial class EditTodoDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditTodoDialog"/> class.
    /// </summary>
    /// <param name="todoItem">The todo item to edit.</param>
    public EditTodoDialog(TodoItem todoItem)
    {
        _ = todoItem ?? throw new System.ArgumentNullException(nameof(todoItem));

        InitializeComponent();

        // Initialize form with current values
        txtTitle.Text = todoItem.Title;
        txtDescription.Text = todoItem.Description;

        if (todoItem.DueDate.HasValue)
        {
            dpDueDate.SelectedDate = todoItem.DueDate.Value.DateTime;
            txtDueTime.Text = todoItem.DueDate.Value.ToString("HH:mm");
        }

        cmbPriority.ItemsSource = Enum.GetValues<TodoPriority>();
        cmbPriority.SelectedItem = todoItem.Priority;

        // Store original for comparison
        OriginalTodo = todoItem;
    }

    /// <summary>
    /// Gets the original todo item.
    /// </summary>
    public TodoItem OriginalTodo { get; }

    /// <summary>
    /// Gets the updated todo item if changes were made.
    /// </summary>
    public TodoItem? UpdatedTodo { get; private set; }

    /// <summary>
    /// Handles the OK button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void BtnOK_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show("Title is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Create updated todo
        UpdatedTodo = new TodoItem
        {
            Id = OriginalTodo.Id,
            Title = txtTitle.Text,
            Description = txtDescription.Text,
            Priority = (TodoPriority)cmbPriority.SelectedItem,
            CreatedAt = OriginalTodo.CreatedAt,
            IsCompleted = OriginalTodo.IsCompleted,
            Tags = OriginalTodo.Tags
        };

        // Parse due date and time
        if (dpDueDate.SelectedDate.HasValue)
        {
            var dueDate = dpDueDate.SelectedDate.Value;

            if (!string.IsNullOrWhiteSpace(txtDueTime.Text) && TimeSpan.TryParse(txtDueTime.Text, out var time))
            {
                dueDate = dueDate.Add(time);
            }

            UpdatedTodo.DueDate = new DateTimeOffset(dueDate);
        }

        DialogResult = true;
    }

    /// <summary>
    /// Handles the Cancel button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
