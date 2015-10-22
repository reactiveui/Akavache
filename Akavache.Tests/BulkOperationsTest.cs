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
        public void GetShouldWorkWithMultipleKeys()
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

                Assert.Equal(keys.Count(), fixture.GetAllKeys().First().Count());

                var allData = fixture.Get(keys).First();

                Assert.Equal(keys.Count(), allData.Count());
                Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
            }
        }

        [Fact]
        public void GetShouldInvalidateOldKeys()
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

                var allData = fixture.Get(keys).First();
                Assert.Equal(0, allData.Count());
                Assert.Equal(0, fixture.GetAllKeys().First().Count());
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

                Assert.Equal(keys.Count(), fixture.GetAllKeys().First().Count());

                var allData = fixture.Get(keys).First();

                Assert.Equal(keys.Count(), allData.Count());
                Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
            }
        }

        [Fact]
        public void InvalidateShouldTrashMultipleKeys()
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

                Assert.Equal(keys.Count(), fixture.GetAllKeys().First().Count());

                fixture.Invalidate(keys).First();

                Assert.Equal(0, fixture.GetAllKeys().First().Count());
            }
        }
    }

    public abstract class ObjectBulkOperationsTests
    {
        protected abstract IBlobCache CreateBlobCache(string path);

        [Fact]
        public void GetShouldWorkWithMultipleKeys()
        {
            var path = default(string);
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                foreach (var v in keys)
                {
                    fixture.InsertObject(v, data).First();
                }

                Assert.Equal(keys.Count(), fixture.GetAllKeys().First().Count());

                var allData = fixture.GetObjects<Tuple<string, int>>(keys).First();

                Assert.Equal(keys.Count(), allData.Count());
                Assert.True(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2));
            }
        }

        [Fact]
        public void GetShouldInvalidateOldKeys()
        {
            var path = default(string);
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                foreach (var v in keys)
                {
                    fixture.InsertObject(v, data, DateTimeOffset.MinValue).First();
                }

                var allData = fixture.GetObjects<Tuple<string, int>>(keys).First();
                Assert.Equal(0, allData.Count());
                Assert.Equal(0, fixture.GetAllKeys().First().Count());
            }
        }

        [Fact]
        public void InsertShouldWorkWithMultipleKeys()
        {
            var path = default(string);
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                fixture.InsertObjects(keys.ToDictionary(k => k, v => data)).First();

                Assert.Equal(keys.Count(), fixture.GetAllKeys().First().Count());

                var allData = fixture.GetObjects<Tuple<string, int>>(keys).First();

                Assert.Equal(keys.Count(), allData.Count());
                Assert.True(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2));
            }
        }

        [Fact]
        public void InvalidateShouldTrashMultipleKeys()
        {
            var path = default(string);
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                foreach (var v in keys)
                {
                    fixture.InsertObject(v, data).First();
                }

                Assert.Equal(keys.Count(), fixture.GetAllKeys().First().Count());

                fixture.InvalidateObjects<Tuple<string, int>>(keys).First();

                Assert.Equal(0, fixture.GetAllKeys().First().Count());
            }
        }
    }

    class BlockingDisposeBulkCache : BlockingDisposeCache, IObjectBulkBlobCache
    {
        public BlockingDisposeBulkCache(IBlobCache inner) : base(inner) { }

        public IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            return _inner.Insert(keyValuePairs, absoluteExpiration);
        }

        public IObservable<IDictionary<string, byte[]>> Get(IEnumerable<string> keys)
        {
            return _inner.Get(keys);
        }

        public IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys)
        {
            return _inner.GetCreatedAt(keys);
        }

        public IObservable<Unit> Invalidate(IEnumerable<string> keys)
        {
            return _inner.Invalidate(keys);
        }

        public IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            return _inner.InsertObjects(keyValuePairs, absoluteExpiration);
        }

        public IObservable<IDictionary<string, T>> GetObjects<T>(IEnumerable<string> keys)
        {
            return _inner.GetObjects<T>(keys);
        }

        public IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys)
        {
            return _inner.InvalidateObjects<T>(keys);
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            return _inner.InsertObject<T>(key, value, absoluteExpiration);
        }

        public IObservable<T> GetObject<T>(string key)
        {
            return _inner.GetObject<T>(key);
        }

        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return _inner.GetObjectCreatedAt<T>(key);
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            return _inner.GetAllObjects<T>();
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            return _inner.InvalidateObject<T>(key);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            return _inner.InvalidateAllObjects<T>();
        }
    }

    public class InMemoryBlobCacheBulkOperationsTests : BulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new InMemoryBlobCache(RxApp.TaskpoolScheduler));
        }
    }

    public class SqliteBlobCacheBulkOperationsTests : BulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new SQLitePersistentBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }

    public class EncryptedSqliteBlobCacheBulkOperationsTests : BulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new Akavache.Sqlite3.SQLiteEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }

    public class InMemoryBlobCacheObjectBulkOperationsTests : ObjectBulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new InMemoryBlobCache(RxApp.TaskpoolScheduler));
        }
    }

    public class SqliteBlobCacheObjectBulkOperationsTests : ObjectBulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new SQLitePersistentBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }

    public class EncryptedSqliteBlobCacheObjectBulkOperationsTests : ObjectBulkOperationsTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new Akavache.Sqlite3.SQLiteEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }
}
