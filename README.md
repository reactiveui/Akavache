## Akavache: An Asynchronous Key-Value Store for Native Applications 

[![NuGet Stats](https://img.shields.io/nuget/v/akavache.svg)](https://www.nuget.org/packages/akavache) [![Build Status](https://dev.azure.com/dotnet/ReactiveUI/_apis/build/status/Akavache-CI)](https://dev.azure.com/dotnet/ReactiveUI/_build/latest?definitionId=25) [![Code Coverage](https://codecov.io/gh/reactiveui/akavache/branch/master/graph/badge.svg)](https://codecov.io/gh/reactiveui/akavache)
<br>
<a href="https://www.nuget.org/packages/akavache">
        <img src="https://img.shields.io/nuget/dt/akavache.svg">
</a>
<a href="#backers">
        <img src="https://opencollective.com/reactiveui/backers/badge.svg">
</a>
<a href="#sponsors">
        <img src="https://opencollective.com/reactiveui/sponsors/badge.svg">
</a>
<a href="https://reactiveui.net/slack">
        <img src="https://img.shields.io/badge/chat-slack-blue.svg">
</a>

Akavache is an *asynchronous*, *persistent* (i.e. writes to disk) key-value
store created for writing desktop and mobile applications in C#, based on
SQLite3. Akavache is great for both storing important data (i.e. user
settings) as well as cached local data that expires.

![Dat Logo](http://f.cl.ly/items/2R3d1o122m090K0W081L/Akavache.png)

### Where can I use it?

Akavache is currently compatible with:

* Xamarin.iOS / Xamarin.Mac
* Xamarin.Android
* .NET 4.5 Desktop (WPF)
* Windows Phone 8.1 Universal Apps
* Windows 10 (Universal Windows Platform)
* Tizen 4.0

### What does that mean?

Downloading and storing remote data from the internet while still keeping the
UI responsive is a task that nearly every modern application needs to do.
However, many applications that don't take the consideration of caching into
the design from the start often end up with inconsistent, duplicated code for
caching different types of objects.

[Akavache](https://github.com/github/akavache) is a library that makes common app
patterns easy, and unifies caching of different object types (i.e. HTTP
responses vs. JSON objects vs. images).

It's built on a core key-value byte array store (conceptually similar to a
`Dictionary<string, byte[]>`), and on top of that store, extensions are
added to support:

- Arbitrary objects via JSON.NET
- Fetching and loading Images and URLs from the Internet
- Storing and automatically encrypting User Credentials

## Platform-specific notes

* **Xamarin.iOS / Xamarin.Mac** - No issues.

* **Xamarin.Android** - No issues.

* **.NET 4.5 Desktop (WPF)** - No issues.

* **Windows Phone 8.1 Universal Apps** - You must mark your application as `x86`
  or `ARM`, or else you will get a strange runtime error about SQLitePCL_Raw not
  loading correctly. You must *also* ensure that the Microsoft Visual C++ runtime
  is added to your project.

* **Windows 10 (Universal Windows Platform)** - You must mark your application as `x86`
  or `ARM`, or else you will get a strange runtime error about SQLitePCL_Raw not
  loading correctly. You must *also* ensure that the Microsoft Visual C++ runtime
  is added to your project.

* **Tizen 4.0** - No issues.

### Getting Started

Interacting with Akavache is primarily done through an object called
`BlobCache`. At App startup, you must first set your app's name via
`BlobCache.ApplicationName` or `Akavache.Registrations.Start("ApplicationName")` . After setting your app's name, you're ready to save some data.

#### Choose a location
There are four build-in locations, that have some magic applied on some systems:

* `BlobCache.LocalMachine` - Cached data. This data may get deleted without notification.
* `BlobCache.UserAccount` - User settings. Some systems backup this data to the cloud.
* `BlobCache.Secure` - For saving sensitive data - like credentials.
* `BlobCache.InMemory` - A database, kept in memory. The data is stored for the lifetime of the app. 

#### The magic

* **Xamarin.iOS** may remove data, stored in `BlobCache.LocalMachine`, to free up disk space (only if your app is not running). The locations `BlobCache.UserAccount` and `BlobCache.Secure` will be backed up to iCloud and iTunes. (https://developer.apple.com/library/content/documentation/FileManagement/Conceptual/FileSystemProgrammingGuide/FileSystemOverview/FileSystemOverview.html#//apple_ref/doc/uid/TP40010672-CH2-SW1)
* **Xamarin.Android** may also start deleting data, stored in `BlobCache.LocalMachine`, if the system runs out of disk space. It isn't clearly specified if your app could be running while the system is cleaning this up. (https://developer.android.com/reference/android/content/Context.html#getCacheDir%28%29)
* **Windows 10 (UWP)** will replicate `BlobCache.UserAccount` and `BlobCache.Secure` to the cloud and synchronize it to all user devices on which the app is installed (https://msdn.microsoft.com/en-us/library/windows/apps/hh465094.aspx)

#### Let's start off

The most straightforward way to use Akavache is via the object extensions:

```cs
using System.Reactive.Linq;   // IMPORTANT - this makes await work!

// Make sure you set the application name before doing any inserts or gets
Akavache.Registrations.Start("AkavacheExperiment")

var myToaster = new Toaster();
await BlobCache.UserAccount.InsertObject("toaster", myToaster);

//
// ...later, in another part of town...
//

// Using async/await
var toaster = await BlobCache.UserAccount.GetObject<Toaster>("toaster");

// or without async/await
Toaster toaster;

BlobCache.UserAccount.GetObject<Toaster>("toaster")
    .Subscribe(x => toaster = x, ex => Console.WriteLine("No Key!"));
```
### Handling Xamarin Linker

There are two options to ensure the Akavache.Sqlite3 dll will not be removed by Xamarin build tools

#### 1) Add a file to reference the types
```cs
public static class LinkerPreserve
{
  static LinkerPreserve()
  {
    var persistentName = typeof(SQLitePersistentBlobCache).FullName;
    var encryptedName = typeof(SQLiteEncryptedBlobCache).FullName;
  }
}
```

#### 2) Use the following initializer in your cross platform library or in your head project
```cs
Akavache.Registrations.Start("ApplicationName")
```

### Handling Errors

When a key is not present in the cache, GetObject throws a
KeyNotFoundException (or more correctly, OnError's the IObservable). Often,
you would want to return a default value instead of failing:

```cs
Toaster toaster;

try {
    toaster = await BlobCache.UserAccount.GetObject("toaster");
} catch (KeyNotFoundException ex) {
    toaster = new Toaster();
}

// Or without async/await:
toaster = await BlobCache.UserAccount.GetObject<Toaster>("toaster")
    .Catch(Observable.Return(new Toaster()));
```

### Shutting Down

Critical to the integrity of your Akavache cache is the `BlobCache.Shutdown()` method. You *must* call this when your application shuts down. Moreover, be sure to wait for the result:

```cs
BlobCache.Shutdown().Wait();
```

Failure to do this may mean that queued items are not flushed to the cache.

### Using a different SQLitePCL.raw bundle, e.g., Microsoft.AppCenter
- Install the `akavache.sqlite3` nuget instead of `akavache`
- Install the SQLitePCLRaw bundle you want to use, e.g., `SQLitePCLRaw.bundle_green`
- Use `Akavache.Sqlite3.Registrations.Start("ApplicationName", () => SQLitePCL.Batteries_V2.Init());` in your platform projects or in your cross platform project.

```XAML
<PackageReference Include="akavache.sqlite3" Version="6.0.40-g7e90c572c6" />
<PackageReference Include="SQLitePCLRaw.bundle_green" Version="1.1.11" />
```
```cs
Akavache.Sqlite3.Registrations.Start("ApplicationName", () => SQLitePCL.Batteries_V2.Init());
```

For more info about using your own versions of [SqlitePCL.raw](https://github.com/ericsink/SQLitePCL.raw/wiki/Using-multiple-libraries-that-use-SQLitePCL.raw)


### Examining Akavache caches

Using [Akavache Explorer](https://github.com/anaisbetts/AkavacheExplorer), you
can dig into Akavache repos for debugging purposes to see what has been stored.

![](http://f.cl.ly/items/2D3Y0L0k262X0U0y3B0e/Image%202012.05.07%206:57:48%20PM.png)

### What's this Global Variable nonsense? Why can't I use $FAVORITE_IOC_LIBRARY

You totally can. Just instantiate `SQLitePersistentBlobCache` or
`SQLiteEncryptedBlobCache` instead - the static variables are there just to make it
easier to get started.

### DateTime/DateTimeOffset Considerations ###

By default JSON.NET's BSON implementation writes `DateTime` as UTC and reads it back in local time.
To override the reader's behavior you can set `BlobCache.ForcedDateTimeKind` as in the following example:

```cs
// Sets the reader to return DateTime/DateTimeOffset in UTC.
BlobCache.ForcedDateTimeKind = DateTimeKind.Utc;
```

## Basic Method Documentation

Every blob cache supports the basic raw operations given below (some of them are
not implemented directly, but are added on via extension methods):

```cs
/*
 * Get items from the store
 */

// Get a single item
IObservable<byte[]> Get(string key);

// Get a list of items
IObservable<IDictionary<string, byte[]>> Get(IEnumerable<string> keys);

// Get an object serialized via InsertObject
IObservable<T> GetObject<T>(string key);

// Get all objects of type T
IObservable<IEnumerable<T>> GetAllObjects<T>();

// Get a list of objects given a list of keys
IObservable<IDictionary<string, T>> GetObjects<T>(IEnumerable<string> keys);

/*
 * Save items to the store
 */

// Insert a single item
IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null);

// Insert a set of items
IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null);

// Insert a single object
IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null);

// Insert a group of objects
IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null);

/*
 * Remove items from the store
 */

// Delete a single item
IObservable<Unit> Invalidate(string key);

// Delete a list of items
IObservable<Unit> Invalidate(IEnumerable<string> keys);

// Delete a single object (do *not* use Invalidate for items inserted with InsertObject!)
IObservable<Unit> InvalidateObject<T>(string key);

// Deletes a list of objects
IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys);

// Deletes all items (regardless if they are objects or not)
IObservable<Unit> InvalidateAll();

// Deletes all objects of type T
IObservable<Unit> InvalidateAllObjects<T>();

/*
 * Get Metadata about items
 */

// Return a list of all keys. Use for debugging purposes only.
IObservable<IEnumerable<string>> GetAllKeys();

// Return the time which an item was created
IObservable<DateTimeOffset?> GetCreatedAt(string key);

// Return the time which an object of type T was created
IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key);

// Return the time which a list of keys were created
IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys);

/*
 * Utility methods
 */

// Attempt to ensure all outstanding operations are written to disk
IObservable<Unit> Flush();

// Preemptively drop all expired keys and run SQLite's VACUUM method on the
// underlying database
IObservable<Unit> Vacuum();
```

## Extension Method Documentation

On top of every `IBlobCache` object, there are extension methods that help with
common application scenarios:

```cs
/*
 * Username / Login Methods (only available on ISecureBlobCache)
 */

// Save login information for the given host
IObservable<Unit> SaveLogin(string user, string password, string host = "default", DateTimeOffset? absoluteExpiration = null);

// Load information for the given host
IObservable<LoginInfo> GetLoginAsync(string host = "default");

// Erase information for the given host
IObservable<Unit> EraseLogin(string host = "default");

/*
 * Downloading and caching URLs and Images
 */

// Download a file as a byte array
IObservable<byte[]> DownloadUrl(string url,
    IDictionary<string, string> headers = null,
    bool fetchAlways = false,
    DateTimeOffset? absoluteExpiration = null);

// Load a given key as an image
IObservable<IBitmap> LoadImage(string key, float? desiredWidth = null, float? desiredHeight = null);

// Download an image from the network and load it
IObservable<IBitmap> LoadImageFromUrl(string url,
    bool fetchAlways = false,
    float? desiredWidth = null,
    float? desiredHeight = null,
    DateTimeOffset? absoluteExpiration = null);

/*
 * Composite operations
 */

// Attempt to return an object from the cache. If the item doesn't
// exist or returns an error, call a Func to return the latest
// version of an object and insert the result in the cache.
IObservable<T> GetOrFetchObject<T>(string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null);

// Like GetOrFetchObject, but isn't async
IObservable<T> GetOrCreateObject<T>(string key, Func<T> fetchFunc, DateTimeOffset? absoluteExpiration = null);

// Immediately return a cached version of an object if available, but *always*
// also execute fetchFunc to retrieve the latest version of an object.
IObservable<T> GetAndFetchLatest<T>(this IBlobCache This,
    string key,
    Func<IObservable<T>> fetchFunc,
    Func<DateTimeOffset, bool> fetchPredicate = null,
    DateTimeOffset? absoluteExpiration = null,
    bool shouldInvalidateOnError = false,
    Func<T, bool> cacheValidationPredicate = null)
```
