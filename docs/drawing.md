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

### **Installation**

```xml
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```

### **Dependencies**

Akavache.Drawing requires:

* Akavache.Core - Core caching functionality
* Splat.Drawing - Cross-platform bitmap abstractions

### **Basic Usage**

#### **1. Initialize Drawing Support**

```csharp
using Akavache.Core;
using Akavache.Drawing;
using Akavache.SystemTextJson;
using Splat;

// Initialize Akavache with drawing support
CacheDatabase.Initialize<SystemJsonSerializer>(builder =>
    builder.WithApplicationName("MyImageApp")
          .WithSqliteProvider()
          .WithSqliteDefaults());

// Register platform-specific bitmap loader using Splat (if needed (Net 8.0+))
AppLocator.CurrentMutable.RegisterPlatformBitmapLoader();
```

#### **2. Load Images from Cache**

```csharp
// Load image from cache
var image = await CacheDatabase.LocalMachine.LoadImage("user_avatar");

// Load with custom sizing
var thumbnail = await CacheDatabase.LocalMachine.LoadImage("user_avatar", 150, 150);

// Load with error handling
try
{
    var profileImage = await CacheDatabase.UserAccount.LoadImage("profile_pic");
    DisplayImage(profileImage);
}
catch (KeyNotFoundException)
{
    // Image not found in cache
    ShowDefaultImage();
}
```

#### **3. Load Images from URLs**

```csharp
// Download and cache image from URL
var imageFromUrl = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("https://example.com/images/photo.jpg");

// With custom expiration
var tempImage = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("https://api.example.com/temp-image.png", 
                     absoluteExpiration: DateTimeOffset.Now.AddHours(1));

// Force fresh download (bypass cache)
var freshImage = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("https://api.example.com/live-feed.jpg", fetchAlways: true);

// With custom key
var namedImage = await CacheDatabase.LocalMachine
   .LoadImageFromUrl("user_background", "https://example.com/bg.jpg");
```

#### **4. Save Images to Cache**

```csharp
// Save image to cache
await CacheDatabase.LocalMachine.SaveImage("user_photo", bitmap);

// Save with expiration
await CacheDatabase.LocalMachine.SaveImage("temp_image", bitmap, 
    DateTimeOffset.Now.AddDays(7));

// Convert bitmap to bytes for manual storage
var imageBytes = await bitmap.ImageToBytes().FirstAsync();
await CacheDatabase.LocalMachine.Insert("raw_image_data", imageBytes);
```

### **Advanced Features**

#### **Batch Image Operations**

```csharp
// Load multiple images at once
var imageKeys = new[] { "image1", "image2", "image3" };
var loadedImages = await CacheDatabase.LocalMachine
   .LoadImages(imageKeys, desiredWidth: 200, desiredHeight: 200)
   .ToList();

foreach (var kvp in loadedImages)
{
    Console.WriteLine($"Loaded {kvp.Key}: {kvp.Value.Width}x{kvp.Value.Height}");
}

// Preload images from URLs (background caching)
var urls = new[]
{
    "https://example.com/image1.jpg",
    "https://example.com/image2.jpg",
    "https://example.com/image3.jpg"
};

await CacheDatabase.LocalMachine.PreloadImagesFromUrls(urls, 
    DateTimeOffset.Now.AddDays(1));
```

#### **Image Fallbacks**

```csharp
// Load image with automatic fallback
var defaultImageBytes = File.ReadAllBytes("default-avatar.png");

var userAvatar = await CacheDatabase.UserAccount
   .LoadImageWithFallback("user_avatar", defaultImageBytes, 100, 100);

// Load from URL with fallback
var profileImage = await CacheDatabase.LocalMachine
   .LoadImageFromUrlWithFallback("https://example.com/profile.jpg", 
                                 defaultImageBytes, 
                                 desiredWidth: 200, 
                                 desiredHeight: 200);
```

#### **Thumbnail Generation**

```csharp
// Create and cache thumbnail from existing image
await CacheDatabase.LocalMachine.CreateAndCacheThumbnail(
    sourceKey: "original_photo",
    thumbnailKey: "photo_thumb", 
    thumbnailWidth: 150, 
    thumbnailHeight: 150,
    absoluteExpiration: DateTimeOffset.Now.AddDays(30));

// Load the cached thumbnail
var thumbnail = await CacheDatabase.LocalMachine.LoadImage("photo_thumb");
```

#### **Image Size Detection**

```csharp
// Get image dimensions without fully loading
var imageSize = await CacheDatabase.LocalMachine.GetImageSize("large_image");
Console.WriteLine($"Image size: {imageSize.Width}x{imageSize.Height}");
Console.WriteLine($"Aspect ratio: {imageSize.AspectRatio:F2}");

// Use size info for layout decisions
if (imageSize.AspectRatio > 1.5)
{
    // Wide image
    SetWideImageLayout();
}
else
{
    // Square or tall image
    SetNormalImageLayout();
}
```

#### **Cache Management**

```csharp
// Clear images matching a pattern
await CacheDatabase.LocalMachine.ClearImageCache(key => key.StartsWith("temp_"));

// Clear all user avatars
await CacheDatabase.UserAccount.ClearImageCache(key => key.Contains("avatar"));

// Clear expired images
await CacheDatabase.LocalMachine.ClearImageCache(key => 
    key.StartsWith("cache_") && IsExpired(key));
```
