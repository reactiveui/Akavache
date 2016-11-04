## Akavache: An Asynchronous Key-Value Store for Native Applications [![Build status](https://ci.appveyor.com/api/projects/status/4kret7d2wqtd47dk/branch/akavache5-master?svg=true)](https://ci.appveyor.com/project/ghuntley/akavache/branch/akavache5-master)


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
* WinRT (Windows Store)
* Windows Phone 8.1 Universal Apps
* Windows 10 (Universal Windows Platform)

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

* **.NET 4.5 Desktop (WPF)** - No issues

* **WinRT (Windows Store)** - You must mark your application as `x86` or `ARM`, or
  else you will get a strange runtime error about SQLitePCL_Raw not loading
  correctly. You must *also* ensure that the Microsoft Visual C++ runtime is added
  to your project. This means that you must submit several versions of your app
  to the Store to support ARM.

* **Windows Phone 8.1 Universal Apps** - You must mark your application as `x86`
  or `ARM`, or else you will get a strange runtime error about SQLitePCL_Raw not
  loading correctly. You must *also* ensure that the Microsoft Visual C++ runtime
  is added to your project.

* **Windows 10 (Universal Windows Platform)** - You must mark your application as `x86`
  or `ARM`, or else you will get a strange runtime error about SQLitePCL_Raw not
  loading correctly. You must *also* ensure that the Microsoft Visual C++ runtime
  is added to your project.

### Getting Started

Interacting with Akavache is primarily done through an object called
`BlobCache`. At App startup, you must first set your app's name via
`BlobCache.ApplicationName` - on the desktop, your application's data will be
stored in `%AppData%\[ApplicationName]` and
`%LocalAppData%\[ApplicationName]`. Store data that should be shared between
different machines in `BlobCache.UserAccount` and store data that is
throwaway or per-machine (such as images) in `BlobCache.LocalMachine`.

The most straightforward way to use Akavache is via the object extensions:

```cs
using System.Reactive.Linq;   // IMPORTANT - this makes await work!

// Make sure you set the application name before doing any inserts or gets
BlobCache.ApplicationName = "AkavacheExperiment";

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

### Handling Errors

When a key is not present in the cache, GetObject throws a
KeyNotFoundException (or more correctly, OnError's the IObservable). Often,
you would want to return a default value instead of failing:

```cs
Toaster toaster;

try {
    toaster = await BlobCache.UserAccount.GetObjectAsync("toaster");
} catch (KeyNotFoundException ex) {
    toaster = new Toaster();
}

// Or without async/await:
toaster = await BlobCache.UserAccount.GetObjectAsync<Toaster>("toaster")
    .Catch(Observable.Return(new Toaster()));
```

### Shutting Down

Critical to the integrity of your Akavache cache is the `BlobCache.Shutdown()` method. You *must* call this when your application shuts down. Moreover, be sure to wait for the result:

```cs
BlobCache.Shutdown().Wait();
```

Failure to do this may mean that queued items are not flushed to the cache.

### Examining Akavache caches

Using [Akavache Explorer](https://github.com/paulcbetts/AkavacheExplorer), you
can dig into Akavache repos for debugging purposes to see what has been stored.

![](http://f.cl.ly/items/2D3Y0L0k262X0U0y3B0e/Image%202012.05.07%206:57:48%20PM.png)

### What's this Global Variable nonsense? Why can't I use $FAVORITE_IOC_LIBRARY

You totally can. Just instantiate `SQLitePersistentBlobCache` or
`SQLiteEncryptedBlobCache` instead - the static variables are there just to make it
easier to get started.



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
IObservable<T> GetAndFetchLatest<T>(string key,
    Func<Task<T>> fetchFunc,
    Func<DateTimeOffset, bool> fetchPredicate = null,
    DateTimeOffset? absoluteExpiration = null);
```
