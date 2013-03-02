using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Xunit;
using Xunit.Extensions;

namespace Akavache.Tests
{
    /// <summary>
    /// These are tests specific to the persistent blob cache base class. Most of the tests are in 
    /// BlobCacheFixture. These tests are the ones that don't apply to the SQL stuff.
    /// </summary>
    public class PersistentBlobCacheTests
    {
        public class CrazyStressTests
        {
            [Theory]
            [PropertyData("CacheFuncs")]
            public void JustStartHammeringThis(Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache> factory)
            {
                // Just a crazy test that hammers the cache.
                const int timeout = 5; // seconds
                var exceptions = new ConcurrentBag<Exception>();
                long counter = long.MinValue;
                var cache = factory("somepath", _ => {});
                var allThreads = new List<Thread>();
                bool running = true;
                var cacheKeys = new[]
                {
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString()
                };
                Func<string, Action<string>, Thread> createThread =
                    (key, action) => new Thread(() =>
                    {
                        try
                        {
                            while (running || exceptions.Count == 0)
                            {
                                action(key);
                            }
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        }
                    });

                // Just insert crap.
                allThreads.AddRange(cacheKeys.Select(
                    key => createThread(key, k => cache.Insert(k, BitConverter.GetBytes(counter++)))));
                // Just invalidate crap
                allThreads.AddRange(cacheKeys.Select(
                    key => createThread(key, k => cache.Invalidate(k))));
                // Just insert then crap
                allThreads.AddRange(cacheKeys.Select(
                    key => createThread(key, k =>
                    {
                        cache.Insert(key, BitConverter.GetBytes(counter++));
                        cache.Invalidate(key);
                    })));
                // Just read crap
                allThreads.AddRange(cacheKeys.Select(
                    key => createThread(key, k => cache.GetAsync(key).First())));

                // Just insert and read crap
                allThreads.AddRange(cacheKeys.Select(
                    key => createThread(key, k =>
                    {
                        cache.Insert(key, BitConverter.GetBytes(counter++));
                        cache.GetAsync(key).First();
                    })));

                allThreads.ForEach(t => t.Start());

                var endTime = DateTime.UtcNow.AddSeconds(timeout);
                while (exceptions.Count == 0 && DateTime.UtcNow < endTime)
                {
                    Thread.Sleep(1);
                }
                running = false;
                allThreads.ForEach(t => t.Join(100));

                Console.WriteLine(exceptions.Count);
                foreach (var exception in exceptions)
                {
                    Console.WriteLine(exception.Message);
                }
                Assert.Empty(exceptions);
            }

            public static IEnumerable<Object[]> CacheFuncs
            {
                get
                {
                    yield return new object[]
                    {
                        new Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache>(
                            (cacheDir, invalidateCallback) =>
                                new PersistentBlobCacheTester(cacheDir, invalidateCallback))
                    };
                    yield return new object[]
                    {
                        new Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache>(
                            (cacheDir, invalidateCallback) =>
                                new EncryptedBlobCacheTester(cacheDir, invalidateCallback))
                    };
                }
            }
        }

        public class TheDisposeMethod
        {
            /// <summary>
            /// Sorry, this is a fucking complicated test. But rooting out race conditions is hard like that.
            /// This ensures that Dispose doesn't cause existing operations to error out.
            /// </summary>
            [Theory]
            [PropertyData("CacheFuncs")]
            public void DisposeDoesNotCauseNullReferenceException(
                Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache> factory)
            {
                Exception exception = null;
                var invalidateStarted = new ManualResetEvent(false);
                var disposeComplete = new ManualResetEvent(false);
                var invalidateAction = new Action<AsyncSubject<byte[]>>(_ =>
                {
                    // A bit of a cheat since we're relying on an implementation detail. This signals 
                    // the Dispose thread and the insert thread to continue.
                    invalidateStarted.Set();
                    // We let Dispose happen now. Want to make sure it doesn't cause problems 
                    // with completing the insert.
                    disposeComplete.WaitOne(100);
                });
                var cache = factory("somepath", invalidateAction);
                cache.Insert("key", new byte[] {13}, DateTimeOffset.Now);

                // This thread will hold a looong lock on MemoizedRequests
                var invalidateThread = new Thread(_ => cache.Invalidate("key"));
                var insertThread = new Thread(_ =>
                {
                    try
                    {
                        // Make sure the first thread has time to obtain the lock.
                        invalidateStarted.WaitOne();
                        // This thread is going to get blocked by the lock.
                        cache.Insert("key", new byte[] {13});
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                });
                var disposeThread = new Thread(_ =>
                {
                    invalidateStarted.WaitOne(500);
                    cache.Dispose();
                    disposeComplete.Set();
                });

                invalidateThread.Start();
                while (!invalidateThread.IsAlive)
                {
                }
                insertThread.Start();
                while (!insertThread.IsAlive)
                {
                }
                disposeThread.Start();
                while (!disposeThread.IsAlive)
                {
                }
                invalidateThread.Join();
                insertThread.Join();
                disposeThread.Join();

                Assert.Null(exception);
            }

            public static IEnumerable<Object[]> CacheFuncs
            {
                get
                {
                    yield return new object[]
                    {
                        new Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache>(
                            (cacheDir, invalidateCallback) =>
                                new PersistentBlobCacheTester(cacheDir, invalidateCallback))
                    };
                    yield return new object[]
                    {
                        new Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache>(
                            (cacheDir, invalidateCallback) =>
                                new EncryptedBlobCacheTester(cacheDir, invalidateCallback))
                    };
                }
            }
        }

        public class PersistentBlobCacheTester : PersistentBlobCache
        {
            public PersistentBlobCacheTester(
                string cacheDirectory = null,
                Action<AsyncSubject<byte[]>> invalidatedCallback = null)
                : base(cacheDirectory, null, null, invalidatedCallback)
            {
            }
        }

        public class EncryptedBlobCacheTester : EncryptedBlobCache
        {
            public EncryptedBlobCacheTester(
                string cacheDirectory = null,
                Action<AsyncSubject<byte[]>> invalidatedCallback = null)
                : base(cacheDirectory, null, null, invalidatedCallback)
            {
            }
        }
    }
}
