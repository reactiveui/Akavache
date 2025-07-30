# Akavache.Drawing

Drawing and bitmap support for Akavache using Splat.Drawing.

## Overview

This package provides image caching and bitmap manipulation functionality for Akavache, offering feature parity with Akavache.Drawing plus additional enhancements.

## Features

### Core Functionality
- ? Load images from cache
- ? Load images from URLs with caching
- ? Save images to cache
- ? Image validation and error handling
- ? Custom image sizing support
- ? Multiple image format support (PNG, JPEG, GIF, BMP, WebP)

### Advanced Features
- ? Batch image loading
- ? Image preloading from URLs
- ? Fallback image support
- ? Thumbnail generation and caching
- ? Image size detection
- ? Pattern-based cache clearing

### Migration Support
- ? Akavache.Drawing compatibility layer
- ? Same method signatures and behavior
- ? Drop-in replacement capability

## Installation

```xml
<PackageReference Include="Akavache.Drawing" Version="1.0.0" />
```

## Usage

### Basic Setup

```csharp
using Akavache.Core;
using Akavache.Drawing;

// Initialize drawing support
Registrations.Initialize();

// Set up cache
CacheDatabase.ApplicationName = "MyApp";
CacheDatabase.LocalMachine = new SqliteBlobCache("cache.db");
```

### Load Images

```csharp
// Load image from cache
var image = await CacheDatabase.LocalMachine.LoadImage("myImageKey");

// Load image from URL
var imageFromUrl = await CacheDatabase.LocalMachine.LoadImageFromUrl("https://example.com/image.jpg");

// Load with custom sizing
var thumbnail = await CacheDatabase.LocalMachine.LoadImage("myImageKey", 150, 150);
```

### Save Images

```csharp
// Save image to cache
await CacheDatabase.LocalMachine.SaveImage("myImageKey", bitmap);

// Save with expiration
await CacheDatabase.LocalMachine.SaveImage("myImageKey", bitmap, DateTimeOffset.Now.AddDays(7));
```

### Advanced Operations

```csharp
// Load multiple images
var images = await CacheDatabase.LocalMachine
    .LoadImages(new[] { "image1", "image2", "image3" })
    .ToList();

// Load with fallback
var imageWithFallback = await CacheDatabase.LocalMachine
    .LoadImageWithFallback("imageKey", fallbackImageBytes);

// Create and cache thumbnail
await CacheDatabase.LocalMachine
    .CreateAndCacheThumbnail("sourceKey", "thumbKey", 150, 150);

// Get image size without loading
var size = await CacheDatabase.LocalMachine.GetImageSize("imageKey");

// Preload images from URLs
await CacheDatabase.LocalMachine.PreloadImagesFromUrls(urls);
```

### Migration from Akavache.Drawing

Akavache.Drawing provides direct compatibility:

```csharp
// Old Akavache.Drawing code:
var image = await BlobCache.LocalMachine.LoadImage("key");
var imageFromUrl = await BlobCache.LocalMachine.LoadImageFromUrl("url");

// New Akavache.Drawing code (same API):
var image = await CacheDatabase.LocalMachine.LoadImage("key");
var imageFromUrl = await CacheDatabase.LocalMachine.LoadImageFromUrl("url");
```

## Platform Support

- ? .NET Standard 2.0
- ? .NET 8.0
- ? .NET 9.0
- ? Xamarin.iOS/Android/Mac
- ? MAUI
- ? WPF/WinForms
- ? UWP

## Dependencies

- `Akavache.Core` - Core caching functionality
- `Splat.Drawing` - Cross-platform bitmap abstractions

## API Reference

### BitmapImageExtensions

| Method | Description |
|--------|-------------|
| `LoadImage()` | Load image from cache |
| `LoadImageFromUrl()` | Load image from URL with caching |
| `SaveImage()` | Save image to cache |
| `ImageToBytes()` | Convert bitmap to byte array |
| `ThrowOnBadImageBuffer()` | Validate image data |

### ImageCacheExtensions

| Method | Description |
|--------|-------------|
| `LoadImages()` | Load multiple images |
| `PreloadImagesFromUrls()` | Preload images from URLs |
| `LoadImageWithFallback()` | Load with fallback image |
| `LoadImageFromUrlWithFallback()` | Load from URL with fallback |
| `CreateAndCacheThumbnail()` | Generate and cache thumbnails |
| `GetImageSize()` | Get image dimensions |
| `ClearImageCache()` | Clear images by pattern |

### Registrations

| Method | Description |
|--------|-------------|
| `Initialize()` | Initialize drawing support |
| `RegisterBitmapLoader()` | Register platform bitmap loader |

## License

Licensed under the MIT License. See LICENSE file for details.
