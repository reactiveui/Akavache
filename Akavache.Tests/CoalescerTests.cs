using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Akavache.Sqlite3;
using ReactiveUI;
using Xunit;

namespace Akavache.Tests
{
    public class CoalescerTests
    {
        [Fact]
        public void SingleItem()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });

            var queue = fixture.DumpQueue();
            var subj = queue[0].CompletionAsElements;
            var output = subj.CreateCollection();
            Assert.Equal(0, output.Count);

            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(1, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);

            // Make sure the input gets a result when we signal the output's subject
            var outSub = (result[0].CompletionAsElements);

            Assert.Equal(0, output.Count);
            outSub.OnNext(new[] { new CacheElement() { Key = "Foo" }});
            outSub.OnCompleted();
            Assert.Equal(1, output.Count);
        }

        [Fact]
        public void UnrelatedItems()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });
            fixture.Insert(new[] { new CacheElement() { Key = "Bar" } });
            fixture.Invalidate(new[] { "Baz" });

            var queue = fixture.DumpQueue();
            var subj = queue[0].CompletionAsElements;
            var output = subj.CreateCollection();
            Assert.Equal(0, output.Count);

            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(3, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);

            // Make sure the input gets a result when we signal the output's subject
            var outSub = (result[0].CompletionAsElements);

            Assert.Equal(0, output.Count);
            outSub.OnNext(new[] { new CacheElement() { Key = "Foo" }});
            outSub.OnCompleted();
            Assert.Equal(1, output.Count);
        }

        [Fact]
        public void CoalesceUnrelatedSelects()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });
            fixture.Select(new[] { "Bar" });
            fixture.Invalidate(new[] { "Bamf" });
            fixture.Select(new[] { "Baz" });

            var queue = fixture.DumpQueue();
            var output = queue.Where(x => x.OperationType == OperationType.BulkSelectSqliteOperation)
                .Select(x => x.CompletionAsElements)
                .Merge()
                .CreateCollection();
            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(2, result.Count);

            var item = result.Single(x => x.OperationType == OperationType.BulkSelectSqliteOperation);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, item.OperationType);
            Assert.Equal(3, item.ParametersAsKeys.Count());

            // All three of the input Selects should get a value when we signal
            // our output Select
            var outSub = item.CompletionAsElements;
            var fakeResult = new[] {
                new CacheElement() { Key = "Foo" },
                new CacheElement() { Key = "Bar" },
                new CacheElement() { Key = "Baz" },
            };

            Assert.Equal(0, output.Count);
            outSub.OnNext(fakeResult);
            outSub.OnCompleted();
            Assert.Equal(3, output.Count);
        }

        [Fact]
        public void DedupRelatedSelects()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });
            fixture.Select(new[] { "Foo" });
            fixture.Select(new[] { "Bar" });
            fixture.Select(new[] { "Foo" });

            var queue = fixture.DumpQueue();
            var output = queue.Where(x => x.OperationType == OperationType.BulkSelectSqliteOperation)
                .Select(x => x.CompletionAsElements)
                .Merge()
                .CreateCollection();
            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(1, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);
            Assert.Equal(2, result[0].ParametersAsKeys.Count());

            var fakeResult = new[] {
                new CacheElement() { Key = "Foo" },
                new CacheElement() { Key = "Bar" },
            };

            var outSub = result[0].CompletionAsElements;

            Assert.Equal(0, output.Count);
            outSub.OnNext(fakeResult);
            outSub.OnCompleted();
            Assert.Equal(4, output.Count);
        }

        [Fact]
        public void InterpolatedOpsDontGetDeduped()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });
            fixture.Insert(new[] { new CacheElement() { Key = "Foo", Value = new byte[] { 1,2,3 } } });
            fixture.Select(new[] { "Foo" });
            fixture.Insert(new[] { new CacheElement() { Key = "Foo", Value = new byte[] { 4,5,6 } } });

            var queue = fixture.DumpQueue();
            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(4, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);
            Assert.Equal(OperationType.BulkInsertSqliteOperation, result[1].OperationType);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[2].OperationType);
            Assert.Equal(OperationType.BulkInsertSqliteOperation, result[3].OperationType);

            Assert.Equal(1, result[1].ParametersAsElements.First().Value[0]);
            Assert.Equal(4, result[3].ParametersAsElements.First().Value[0]);
        }

        [Fact]
        public async Task GroupedRequestsWithDifferentKeysReturnEmptyResultIfItemsDontExist()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                using (var cache = new SQLitePersistentBlobCache(Path.Combine(path, "sqlite.db")))
                {
                    var queue = new SqliteOperationQueue(cache.Connection, BlobCache.TaskpoolScheduler);
                    var request = queue.Select(new[] { "Foo" });
                    var unrelatedRequest = queue.Select(new[] { "Bar" });

                    cache.ReplaceOperationQueue(queue);

                    Assert.Equal(0, (await request).Count());
                    Assert.Equal(0, (await unrelatedRequest).Count());
                }
            }
        }
    }
}