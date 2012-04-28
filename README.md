## Akavache: An Asynchronous Key-Value Store for Native Applications

Akavache is an *asynchronous*, *persistent* key-value cache created
for writing native desktop and mobile applications in C#. Think of it like
memcached for desktop apps.

## What does that mean?

Downloading and caching remote data from the internet while still keeping the
UI responsive is a task that nearly every modern native application needs to
do. However, many applications that don't take the consideration of caching
into the design from the start often end up with inconsistent, duplicated code
for caching different types of objects. 

[Akavache](github.com/github/akavache) is a library that makes common app
patterns easy, and unifies caching of different object types (i.e. HTTP
responses vs. JSON objects vs. images). 

It's built on a core key-value byte array store (conceptually similar to a
`Dictionary<string, byte[]>`), and on top of that store, extensions are
added to support:

- Arbitrary objects via JSON
- HTTP Requests
- Fetching and loading Images
- Securely storing User Credentials


## An example - consider Twitter for Mac

![](http://f.cl.ly/items/2N2F2n3X2y3n1g3o3e1e/Image%202012.04.27%204:37:37%20PM.png)

When you open Twitter for Mac, you immediately see content, even before the
application finishes talking to Twitter. This content is cached, but the
pattern of when to refresh the data might be different. For the tweets
themselves, the logic might be something like, *"Load the cached data, but
**always** fetch the latest data"*. 

However, for the avatar images, you might have logic like, *"Always load the
cached image, but if the avatar is older than six hours, refresh the image."*

In Akavache, the former might be:

```cs
var tweets = await BlobCache.UserAccount.GetAndFetchLatest("tweets", DownloadSomeTweets());
```

And the latter might be something like:

```cs
var image = await BlobCache.LocalMachine.GetAndFetchLatest(tweet.AvatarUrl, 
    DownloadUrl(tweet.AvatarUrl), 
    createdAt => DateTimeOffset.Now - createdAt > TimeSpan.FromHours(6));
```

## No (Thread.)Sleep 'till Brooklyn

Akavache is non-blocking, via a library called the [Reactive
Extensions](http://msdn.microsoft.com/en-us/data/gg577609) - any operation
that could delay the UI returns an *Observable*, which represents a future
result. 

Akavache also solves several difficult concurrency problems that simplify UI
programming. For example, in the image above, a naive cache implementation
could end up loading the GitHub avatar icon multiple times if the requests
were issued at the same time (which is easy to do if you try to load the
avatar for each tweet). Akavache ensures that exactly *one* network request is
issued. 