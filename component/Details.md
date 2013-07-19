## Akavache: An Asynchronous Key-Value Store for Native Applications

Akavache is an *asynchronous*, *persistent* (i.e. writes to disk) key-value
store created for writing desktop and mobile applications in C#. Think of it
like memcached for desktop apps.

## Where can I use it?

Akavache is currently compatible with .NET 4.0/4.5, Mono 3.0 (including
Xamarin.Mac), Silverlight 5, Windows Phone 7.1/8.0, and WinRT (Metro / Modern
UI / Windows Store / Whatever Microsoft Is Calling That Tablet'y OS Thing That
They Make).

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
