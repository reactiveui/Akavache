using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache.Sqlite3;
using ReactiveUI;
using Xunit;

namespace Akavache.Tests
{
    public abstract class BulkOperationsTests
    {
        protected abstract IBlobCache CreateBlobCache(string path);

        [Fact]
        public void GetAsyncShouldWorkWithMultipleKeys()
        {
            var path = default(string);
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = new byte[] { 0x10, 0x20, 0x30, };
                var keys = new[] { "Foo", "Bar", "Baz", };

                foreach (var v in keys)
                {
                    fixture.Insert(v, data).First();
                }

                Assert.Equal(keys.Count(), fixture.GetAllKeys().Count());

                var allData = fixture.GetAsync(keys).First();

                Assert.Equal(keys.Count(), allData.Count());
            }
        }

        [Fact]
        public void GetAsyncShouldInvalidateOldKeys()
        {
            var path = default(string);
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = new byte[] { 0x10, 0x20, 0x30, };
                var keys = new[] { "Foo", "Bar", "Baz", };

                foreach (var v in keys)
                {
                    fixture.Insert(v, data, DateTimeOffset.MinValue).First();
                }

                var allData = fixture.GetAsync(keys).First();
                Assert.Equal(0, allData.Count());
                Assert.Equal(0, fixture.GetAllKeys().Count());
            }
        }

        [Fact]
        public void InsertShouldWorkWithMultipleKeys()
        {
            var path = default(string);
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = new byte[] { 0x10, 0x20, 0x30, };
                var keys = new[] { "Foo", "Bar", "Baz", };

                fixture.Insert(keys.ToDictionary(k => k, v => data)).First();

                Assert.Equal(keys.Count(), fixture.GetAllKeys().Count());

                var allData = fixture.GetAsync(keys).First();

                Assert.Equal(keys.Count(), allData.Count());
            }
        }


    }

    class BlockingDisposeBulkCache : BlockingDisposeCache, IBulkBlobCache
    {
        public BlockingDisposeBulkCache(IBlobCache inner) : base(inner) { }

        public IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            return _inner.Insert(keyValuePairs, absoluteExpiration);
        }

        public IObservable<IDictionary<string, byte[]>> GetAsync(IEnumerable<string> keys)
        {
            return _inner.GetAsync(keys);
        }

        public IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys)
        {
            return _inner.GetCreatedAt(keys);
        }

        public IObservable<Unit> Invalidate(IEnumerable<string> keys)
        {
            return _inner.Invalidate(keys);
        }
    }

    public class PersistentBlobCacheBulkTests : BulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new TPersistentBlobCache(path));
        }
    }

    public class TestBlobCacheBulkOperationsTests : BulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new TestBlobCache(RxApp.TaskpoolScheduler));
        }
    }

    public class EncryptedBlobCacheBulkOperationsTests : BulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new TEncryptedBlobCache(path));
        }
    }

    public class SqliteBlobCacheBulkOperationsTests : BulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new SqlitePersistentBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }

}
