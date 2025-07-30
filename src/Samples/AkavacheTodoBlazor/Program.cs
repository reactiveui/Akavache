// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using AkavacheTodoBlazor.Components;
using AkavacheTodoBlazor.Services;
using AkavacheTodoBlazor.ViewModels;

var builder = WebApplication.CreateBuilder(args);

// Configure Akavache
ConfigureAkavache();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Akavache and reactive services
builder.Services.AddSingleton<TodoCacheService>();
builder.Services.AddScoped<MainViewModel>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Handle application shutdown
app.Lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        // Save application state and shutdown Akavache properly
        var cacheService = app.Services.GetService<TodoCacheService>();
        if (cacheService != null)
        {
            await cacheService.SaveApplicationState();
        }

        await BlobCache.Shutdown();
    }
    catch (Exception ex)
    {
        // Log the error but don't prevent shutdown
        Console.WriteLine($"Error during shutdown: {ex}");
    }
});

app.Run();

static void ConfigureAkavache()
{
    // Initialize Akavache with System.Text.Json serializer for best performance
    CoreRegistrations.Serializer = new SystemJsonSerializer();

    // Initialize SQLite support - use the new V11 initialization pattern
    BlobCache.Initialize(builder =>
    {
        builder.WithApplicationName("AkavacheTodoBlazor")
               .WithSqliteDefaults();
    });

    // Configure DateTime handling for consistent behavior
    BlobCache.ForcedDateTimeKind = DateTimeKind.Utc;

    // Initialize SQLite
    SQLitePCL.Batteries_V2.Init();
}
