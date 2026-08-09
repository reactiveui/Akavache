// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using AkavacheTodoMaui.ViewModels;

namespace AkavacheTodoMaui.Views;

/// <summary>Edit Todo page for modifying existing todos.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public partial class EditTodoPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="EditTodoPage"/> class.</summary>
    /// <param name="viewModel">The edit todo view model.</param>
    public EditTodoPage(EditTodoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
