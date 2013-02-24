using System;
using System.Collections.Generic;
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
                    yield return new object[]{ 
                        new Func<string, Action<AsyncSubject<byte[]>>, PersistentBlobCache>(
                            (cacheDir, invalidateCallback) => 
                                new PersistentBlobCacheTester(cacheDir, invalidateCallback))
                    };
                    yield return new object[]{ 
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
