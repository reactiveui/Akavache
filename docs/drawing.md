# **Akavache.Drawing**

Akavache.Drawing provides comprehensive image caching and bitmap manipulation functionality for Akavache applications. Built on Splat, it offers cross-platform support for loading, caching, and manipulating images with enhanced features beyond basic blob storage.

### **Features**

* **Image Loading & Caching**: Load images from cache with automatic format detection  
* **URL Image Caching**: Download and cache images from URLs with built-in HTTP support  
* **Image Manipulation**: Resize, crop, and generate thumbnails with caching  
* **Multiple Format Support**: PNG, JPEG, GIF, BMP, WebP, and other common formats  
* **Fallback Support**: Automatic fallback to default images when loading fails  
* **Batch Operations**: Load multiple images efficiently  
* **Size Detection**: Get image dimensions without full loading  
* **Advanced Caching**: Pattern-based cache clearing and preloading  
* **Cross-Platform**: Works on all.NET platforms supported by Akavache

# Tutorial: Mastering Image Caching with Akavache.Drawing

Welcome to the `Akavache.Drawing` guide! This tutorial will walk you through everything you need to know to efficiently cache images and bitmaps in your .NET applications. We'll go from the basics of caching a single online image to building a robust and performant photo gallery service.

`Akavache.Drawing` is a powerful extension built on the cross-platform `Splat.Drawing` library. It provides a comprehensive suite of tools for loading, caching, manipulating, and managing images, going far beyond simple blob storage.

## Chapter 1: Getting Started - Your First Cached Image

Let's start with a quick win. We'll install the library, initialize it, and cache our first image from the internet in just a few steps.

### 1. Installation

First, add the `Akavache.Drawing` package to your project. You will also need a storage backend (like `Akavache.Sqlite3`) and a serializer.

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```

### 2. Initialization

Next, initialize Akavache in your application's startup code. For this tutorial, we'll also register the platform-specific bitmap loader, which is good practice for modern .NET applications.

```csharp
using Akavache.Core;
using Akavache.Drawing;
using Akavache.SystemTextJson;
using Splat;

// Initialize Akavache with drawing support
CacheDatabase.Initialize<SystemTextJsonSerializer>(builder =>
    builder.WithApplicationName("MyImageApp")
          .WithSqliteProvider()
          .WithSqliteDefaults());

// Register platform-specific bitmap loader using Splat (recommended for .NET 8.0+)
AppLocator.CurrentMutable.RegisterPlatformBitmapLoader();
```

### 3. Cache an Image from a URL

Now for the magic. With a single line of code, you can download an image from a URL, and Akavache will automatically save it to the LocalMachine cache. Subsequent calls for the same URL will load the image instantly from the local disk.

```csharp
// This will download the image, save it to the cache, and return it as an IBitmap.
// If you run this line again, it will load directly from the cache.
var imageFromUrl = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("https://aka.ms/dotnet-bot");

// You can now use this 'imageFromUrl' object in your UI.
// For example, in a XAML-based UI: MyImage.Source = imageFromUrl.ToNative();
```

Congratulations! You've just implemented a persistent image cache.

## Chapter 2: The Image Lifecycle - Core Operations

There are three primary operations you'll use to manage the lifecycle of cached images.

### 1. LoadImageFromUrl - Caching Remote Images

This is the method you'll use most often. It's the workhorse for downloading and caching images from the internet.

```csharp
// Simple download and cache
var image = await CacheDatabase.LocalMachine.LoadImageFromUrl("https://example.com/photo.jpg");

// You can also specify a custom key if you don't want to use the URL as the key.
var userBackground = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("user_background__123", "https://example.com/bg.jpg");

// For temporary images, you can set an expiration time.
var tempImage = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("https://api.example.com/temp-image.png",
                      absoluteExpiration: DateTimeOffset.Now.AddHours(1));

// To bypass the cache and force a fresh download, use 'fetchAlways'.
var freshImage = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("https://api.example.com/live-feed.jpg", fetchAlways: true);
```

### 2. SaveImage - Caching Local Images

Use SaveImage when the image originates from the user's device, such as a photo they've just taken, an image they've selected from their library, or a bitmap you've generated in code.

```csharp
// Assume 'userSelectedBitmap' is an IBitmap object from a file picker or camera.
IBitmap userSelectedBitmap = ...;

// Save the user's photo to the UserAccount cache.
await CacheDatabase.UserAccount.SaveImage("user_photo", userSelectedBitmap);

// You can also set an expiration date, for example, for a temporary edited image.
await CacheDatabase.LocalMachine.SaveImage("temp_edit", userSelectedBitmap,
    DateTimeOffset.Now.AddDays(7));
```

### 3. LoadImage - Retrieving from Cache

This is the universal method for retrieving an image from the cache, regardless of how it got there.

```csharp
try
{
    // Load the user's profile picture from the UserAccount cache.
    var profileImage = await CacheDatabase.UserAccount.LoadImage("profile_pic");
    DisplayImage(profileImage);
}
catch (KeyNotFoundException)
{
    // This is the correct way to handle a cache miss.
    ShowDefaultImage();
}

// You can also request a specific size. Akavache will resize the image for you.
var thumbnail = await CacheDatabase.LocalMachine.LoadImage("user_avatar", 150, 150);
```

## Chapter 3: Practical Application - Building a Robust Avatar Service

Let's build something real. A common requirement is to display user avatars, but what happens if the user hasn't set one, or if the network is down? We need to handle this gracefully by showing a default placeholder image. The LoadImageFromUrlWithFallback method is perfect for this.

### AvatarService.cs

```csharp
using Akavache;
using Splat;
using System.IO;
using System.Threading.Tasks;

public class AvatarService
{
    private readonly IBlobCache _cache;
    private readonly byte[] _defaultAvatarBytes;

    public AvatarService(IBlobCache? cache = null)
    {
        _cache = cache ?? CacheDatabase.UserAccount;

        // In a real app, you would load this from embedded resources.
        _defaultAvatarBytes = File.ReadAllBytes("default-avatar.png");
    }

    /// <summary>
    /// Gets a user's avatar. It tries to load from the cache, then fetches from the URL.
    /// If both fail, it returns a default placeholder avatar.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="avatarUrl">The URL of the user's avatar.</param>
    /// <param name="size">The desired size of the avatar.</param>
    /// <returns>An IBitmap of the avatar or a default placeholder.</returns>
    public async Task<IBitmap> GetUserAvatar(string userId, string avatarUrl, float size)
    {
        var cacheKey = $"avatar_{userId}";

        // This powerful method handles everything:
        // 1. Tries to load the image from the cache using the key "avatar_{userId}".
        // 2. If not found, it tries to download from 'avatarUrl'.
        // 3. If the download is successful, it caches the image with the key.
        // 4. If the download fails (e.g., network error, 404), it uses '_defaultAvatarBytes'.
        // 5. It resizes the final image to the desired size.
        return await _cache.LoadImageFromUrlWithFallback(
            cacheKey,
            avatarUrl,
            _defaultAvatarBytes,
            desiredWidth: size,
            desiredHeight: size);
    }
}
```

This service is now resilient and provides a professional user experience by ensuring an image is always available.

## Chapter 4: Advanced Techniques - Performance and Management

Now let's explore some advanced features to make your application faster and more efficient.

### 1. Preloading Images for a Gallery

Imagine a user is about to view a photo gallery. To make the gallery appear instantly, you can "warm" the cache by preloading the images in the background before they even navigate to the screen.

```csharp
// A list of URLs for an upcoming photo gallery.
var photoUrls = new[]
{
    "https://example.com/image1.jpg",
    "https://example.com/image2.jpg",
    "https://example.com/image3.jpg"
};

// This will download and cache all the images in the background without returning them.
// When the user opens the gallery, LoadImageFromUrl will be instantaneous.
await CacheDatabase.LocalMachine.PreloadImagesFromUrls(photoUrls,
    DateTimeOffset.Now.AddDays(1));
```

### 2. Generating and Caching Thumbnails

Displaying full-size images in a list or grid is slow and memory-intensive. The best practice is to show small thumbnails and only load the full image when the user taps on one. CreateAndCacheThumbnail makes this easy.

```csharp
// Assume "original_photo" is already in the cache.
string sourceKey = "original_photo";
string thumbnailKey = "photo_thumb";

// Create and cache a 150x150 thumbnail from the original image.
await CacheDatabase.LocalMachine.CreateAndCacheThumbnail(
    sourceKey: sourceKey,
    thumbnailKey: thumbnailKey,
    thumbnailWidth: 150,
    thumbnailHeight: 150,
    absoluteExpiration: DateTimeOffset.Now.AddDays(30));

// Now, in your list view, you can load the much smaller thumbnail.
var thumbnail = await CacheDatabase.LocalMachine.LoadImage(thumbnailKey);
```

### 3. Getting Image Dimensions without Loading

Sometimes you need to know the size of an image for layout calculations before you actually load the full bitmap into memory. GetImageSize reads just the image metadata, making it extremely fast and memory-efficient.

```csharp
var imageSize = await CacheDatabase.LocalMachine.GetImageSize("large_image");

if (imageSize != null)
{
    System.Console.WriteLine($"Image size: {imageSize.Width}x{imageSize.Height}");
    System.Console.WriteLine($"Aspect ratio: {imageSize.AspectRatio:F2}");

    // Use this information to adjust your UI layout.
    if (imageSize.AspectRatio > 1.5)
    {
        SetWideImageLayout();
    }
}
```

### 4. Managing the Cache

It's important to clean up the cache to manage storage space. ClearImageCache allows you to remove images based on a predicate.

```csharp
// Clear all images with keys that start with "temp_"
await CacheDatabase.LocalMachine.ClearImageCache(key => key.StartsWith("temp_"));

// Clear all cached avatars from the UserAccount cache
await CacheDatabase.UserAccount.ClearImageCache(key => key.Contains("avatar"));
```

## Conclusion & API Quick Reference

You now have a comprehensive understanding of how to use Akavache.Drawing to build fast, robust, and efficient applications. You've learned the core lifecycle operations, how to build a practical avatar service with fallbacks, and how to apply advanced techniques for performance and management.

For quick reference, here is a summary of the available features.

* Image Loading & Caching: Load images from cache with automatic format detection.
* URL Image Caching: Download and cache images from URLs with built-in HTTP support.
* Image Manipulation: Resize, crop, and generate thumbnails with caching.
* Multiple Format Support: PNG, JPEG, GIF, BMP, WebP, and other common formats.
* Fallback Support: Automatic fallback to default images when loading fails.
* Batch Operations: Load multiple images efficiently.
* Size Detection: Get image dimensions without full loading.
* Advanced Caching: Pattern-based cache clearing and preloading.
* Cross-Platform: Works on all.NET platforms supported by Akavache.
