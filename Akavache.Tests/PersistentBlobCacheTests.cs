using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Akavache.Deprecated;
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
            public void CreateAndReadAtSameTimeTest(
                string cacheType,
                Func<string, Action<AsyncSubject<byte[]>>, 
                PersistentBlobCache> factory)
            {
                var cache = factory("somepath", _ => {});
                var stresser = new Stresser(new Action<string>[]
                {
                    key => cache.Insert(key, Stresser.RandomData()),
                    key => cache.Get(key).First()
                });

                var exceptions = stresser.RunActions(TimeSpan.FromSeconds(2));

                
                cache.Dispose();
                cache.Shutdown.Wait();
                Assert.Equal("", String.Join(",", exceptions));
            }

            // This test is cuuurraazzy!
            [Theory]
            [PropertyData("CacheFuncs")]
            public void CreateReadAndInvalidateAtSameTimeTest(
                string cacheType,
                Func<string, Action<AsyncSubject<byte[]>>, 
                PersistentBlobCache> factory)
            {
                var cache = factory("somepath", _ => {});
                var stresser = new Stresser(new Action<string>[]
                {
                    key => cache.Insert(key, Stresser.RandomData()),
                    key =>
                    {
                        cache.Insert(key, Stresser.RandomData());
                        cache.Get(key).First();
                    },
                    key => cache.Get(key).First(),
                    key => cache.Invalidate(key).First()
                }, uniqueKeyCount: 2);

                var exceptions = stresser.RunActions(TimeSpan.FromSeconds(2));

                cache.Dispose();
                cache.Shutdown.Wait();
                Assert.Equal("", String.Join(",", exceptions));
            }

            public static IEnumerable<Object[]> CacheFuncs
            {
                get
                {
                    yield return new object[]
                    {
                        "PersistentCache",
                        new Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache>(
                            (cacheDir, invalidateCallback) =>
                                new PersistentBlobCacheTester("pers-" + cacheDir, invalidateCallback))
                    };
                    yield return new object[]
                    {
                        "EncryptedCache",
                        new Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache>(
                            (cacheDir, invalidateCallback) =>
                                new EncryptedBlobCacheTester("encr-" + cacheDir, invalidateCallback))
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
                cache.Shutdown.Wait();
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
                : base(cacheDirectory, null, null, null, invalidatedCallback)
            {
            }
        }
    }
}
