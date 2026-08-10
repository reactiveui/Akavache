// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheTodoMaui;

/// <summary>The shell that hosts the application's navigation structure.</summary>
/// <seealso cref="Shell" />
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public partial class AppShell : Shell
{
    /// <summary>Initializes a new instance of the <see cref="AppShell"/> class.</summary>
    public AppShell() => InitializeComponent();
}
