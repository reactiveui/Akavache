// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace AkavacheTodoMaui.Models;

/// <summary>
/// Represents application settings that will be cached using Akavache.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the theme preference.
    /// </summary>
    [JsonPropertyName("theme")]
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled.
    /// </summary>
    [JsonPropertyName("notificationsEnabled")]
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the notification time before due date (in minutes).
    /// </summary>
    [JsonPropertyName("notificationMinutes")]
    public int NotificationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the default todo priority.
    /// </summary>
    [JsonPropertyName("defaultPriority")]
    public TodoPriority DefaultPriority { get; set; } = TodoPriority.Medium;

    /// <summary>
    /// Gets or sets a value indicating whether to show completed todos.
    /// </summary>
    [JsonPropertyName("showCompleted")]
    public bool ShowCompleted { get; set; } = true;

    /// <summary>
    /// Gets or sets the sort order for todos.
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public TodoSortOrder SortOrder { get; set; } = TodoSortOrder.DueDate;

    /// <summary>
    /// Gets or sets the user's name.
    /// </summary>
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = "User";

    /// <summary>
    /// Gets or sets the last application usage timestamp.
    /// </summary>
    [JsonPropertyName("lastUsed")]
    public DateTimeOffset LastUsed { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the cache expiration time in hours for todos.
    /// </summary>
    [JsonPropertyName("cacheExpirationHours")]
    public int CacheExpirationHours { get; set; } = 24;
}

/// <summary>
/// Represents available app themes.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Follow system theme.
    /// </summary>
    System = 0,

    /// <summary>
    /// Light theme.
    /// </summary>
    Light = 1,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark = 2
}

/// <summary>
/// Represents todo sort orders.
/// </summary>
public enum TodoSortOrder
{
    /// <summary>
    /// Sort by creation date.
    /// </summary>
    CreatedDate = 1,

    /// <summary>
    /// Sort by due date.
    /// </summary>
    DueDate = 2,

    /// <summary>
    /// Sort by priority.
    /// </summary>
    Priority = 3,

    /// <summary>
    /// Sort alphabetically by title.
    /// </summary>
    Title = 4
}
