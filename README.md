## Akavache: An Asynchronous Key-Value Store for Native Applications

Akavache is an *asynchronous*, *persistent* (i.e. writes to disk) key-value
store created for writing desktop and mobile applications in C#, based on
SQLite3. Akavache is great for both storing important data (i.e. user
settings) as well as cached local data that expires.

![Dat Logo](http://f.cl.ly/items/2R3d1o122m090K0W081L/Akavache.png)

### Where can I use it?

Akavache is currently compatible with:

* Xamarin.iOS / Xamarin.Mac 32-bit
* Xamarin.Android
* .NET 4.5 Desktop (WPF)
* Windows Phone 8
* WinRT (Windows Store)
* Windows Phone 8.1 Universal Apps

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

* **Xamarin.iOS / Xamarin.Mac 32-bit** - No issues.

* **Xamarin.Android** - No issues.

* **.NET 4.5 Desktop (WPF)** - You must mark your application as `x86`, or else
  you will get a strange runtime error about SQLitePCL_Raw not loading correctly.

* **Windows Phone 8.0** - You must mark your application as `x86` or `ARM`, or
  else you will get a strange runtime error about SQLitePCL_Raw not loading
  correctly.

* **WinRT (Windows Store)** - You must mark your application as `x86` or `ARM`, or
  else you will get a strange runtime error about SQLitePCL_Raw not loading
  correctly. You must *also* ensure that the Microsoft Visual C++ runtime is added
  to your project. This means that you must submit several versions of your app
  to the Store to support ARM.

* **Windows Phone 8.1 Universal Apps** - You must mark your application as `x86`
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
using System.Reactive;   // IMPORTANT - this makes await work!

var myToaster = new Toaster();
await BlobCache.UserAccount.InsertObject("toaster", myToaster);

//
// ...later, in another part of town...
//

// Using async/await
var toaster = await BlobCache.UserAccount.GetObjectAsync<Toaster>("toaster");

// or without async/await
Toaster toaster;

BlobCache.UserAccount.GetObjectAsync<Toaster>("toaster")
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

### Examining Akavache caches

Using [Akavache Explorer](https://github.com/paulcbetts/AkavacheExplorer), you
can dig into Akavache repos for debugging purposes to see what has been stored.

![](http://f.cl.ly/items/2D3Y0L0k262X0U0y3B0e/Image%202012.05.07%206:57:48%20PM.png)

### What's this Global Variable nonsense? Why can't I use $FAVORITE_IOC_LIBRARY

You totally can. Just instantiate `SQLitePersistentBlobCache` or
`SQLiteEncryptedBlobCache` instead - the static variables are there just to make it
easier to get started.
