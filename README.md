## Akavache: An Asynchronous Key-Value Store for Native Applications

Akavache is an *asynchronous*, *persistent* (i.e. writes to disk) key-value
store created for writing desktop and mobile applications in C#. Think of it
like memcached for desktop apps.

![Dat Logo](http://f.cl.ly/items/2R3d1o122m090K0W081L/Akavache.png)

## Where can I use it?

Akavache is currently compatible with:

* Xamarin.iOS / Xamarin.Android
* Mono 3.x (including Xamarin.Mac)
* .NET 4.5 Desktop (WPF)
* Windows Phone 8
* WinRT (Windows Store)

## What does that mean?

Downloading and caching remote data from the internet while still keeping the
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
- HTTP Requests
- Fetching and loading Images
- Securely storing User Credentials

## Getting Started

Interacting with Akavache is primarily done through an object called
`BlobCache`. At App startup, you must first set your app's name via
`BlobCache.ApplicationName` - on the desktop, your application's data will be
stored in `%AppData%\[ApplicationName]` and
`%LocalAppData%\[ApplicationName]`. Store data that should be shared between
different machines in `BlobCache.UserAccount` and store data that is
throwaway or per-machine (such as images) in `BlobCache.LocalMachine`.

The most straightforward way to use Akavache is via the object extensions:

```cs
var myToaster = new Toaster();
BlobCache.UserAccount.InsertObject("toaster", myToaster);

//
// ...later, in another part of town...
//

// Using async/await
var toaster = await BlobCache.UserAccount.GetObjectAsync("toaster");

// or without async/await
Toaster toaster;

BlobCache.UserAccount.GetObjectAsync("toaster")
    .Subscribe(x => toaster = x, ex => Console.WriteLine("No Key!"))
```

## Use SQLite3!

Akavache also ships with a separate `IBlobCache` implementation in the
`akavache.sqlite3` NuGet package which supports every platform except for
WP8.

While the default implementation is still quite usable, the SQLite3
implementation has a number of speed and concurrency advantages, and is
**recommended for all new applications**. It's not the default because it
requires a native DLL (`sqlite3.dll`) which is a bit more work to set up. See
[the Sqlite3 README](/Akavache.Sqlite3/sqlite3-hint.txt) for more info.

To use it, simply include `Akavache.Sqlite3.dll`

## Handling Errors

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
toaster = await BlobCache.UserAccount.GetObjectAsync("toaster");
    .Catch(Observable.Return(new Toaster()));
```

## Examining Akavache caches

Using [Akavache Explorer](https://github.com/xpaulbettsx/AkavacheExplorer), you
can dig into Akavache repos for debugging purposes to see what has been stored.

![](http://f.cl.ly/items/2D3Y0L0k262X0U0y3B0e/Image%202012.05.07%206:57:48%20PM.png)

## What's this Global Variable nonsense? Why can't I use $FAVORITE_IOC_LIBRARY

You totally can. Just instantiate `PersistentBlobCache` or
`EncryptedBlobCache` instead - the static variables are there just to make it
easier to get started.

## Unit Testing with Akavache

By default, if Akavache detects that it is being run in a unit test runner, it
will use the `TestBlobCache` implementation instead of the default
implementation. `TestBlobCache` implements `IBlobCache` in memory
synchronously using a Dictionary instead of persisting to disk.

This class can be explicitly created as well, and initialized to have specific
contents to test cache hit / cache miss scenarios. Use the
`TestBlobCache.OverrideGlobals` method to temporarily replace the
`BlobCache.UserAccount` variables with a specific TestBlobCache.

Testing expiration can also be done, using Rx's `TestScheduler`:

```cs
[Fact]
public void TestSomeExpirationStuff()
{
    (new TestScheduler()).With(sched => {
        using (cache = TestBlobCache.OverrideGlobals(null, sched)) {
            cache.Insert("foo", new byte[] { 1,2,3 }, TimeSpan.FromMilliseconds(100));

            sched.AdvanceByMs(50);

            var result = cache.GetAsync("foo").First();
            Assert.Equal(1, result[0]);

            // Fast forward to t=200ms, after the cache entry is expired
            sched.AdvanceByMs(150);

            bool didThrow;
            try {
                // This should now throw KeyNotFoundException
                result = cache.GetAsync("foo").First();
                didThrow = false;
            } catch (Exception ex) {
                didThrow = true;
            }

            Assert.True(didThrow);
        }
    });
}
```
