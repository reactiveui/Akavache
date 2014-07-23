using System;
using System.Collections.Generic;
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
            var subj = queue[0].Item3 as AsyncSubject<IEnumerable<CacheElement>>;
            var output = subj.CreateCollection();
            Assert.Equal(0, output.Count);

            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(1, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].Item1);

            // Make sure the input gets a result when we signal the output's subject
            var outSub = ((AsyncSubject<IEnumerable<CacheElement>>)result[0].Item3);

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
            var subj = queue[0].Item3 as AsyncSubject<IEnumerable<CacheElement>>;
            var output = subj.CreateCollection();
            Assert.Equal(0, output.Count);

            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(3, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].Item1);

            // Make sure the input gets a result when we signal the output's subject
            var outSub = ((AsyncSubject<IEnumerable<CacheElement>>)result[0].Item3);

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
            var output = queue.Where(x => x.Item1 == OperationType.BulkSelectSqliteOperation)
                .Select(x => (AsyncSubject<IEnumerable<CacheElement>>)x.Item3)
                .Merge()
                .CreateCollection();
            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(2, result.Count);

            var item = result.Single(x => x.Item1 == OperationType.BulkSelectSqliteOperation);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, item.Item1);
            Assert.Equal(3, item.Item2.Cast<string>().Count());

            // All three of the input Selects should get a value when we signal
            // our output Select
            var outSub = ((AsyncSubject<IEnumerable<CacheElement>>)item.Item3);
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
            var output = queue.Where(x => x.Item1 == OperationType.BulkSelectSqliteOperation)
                .Select(x => (AsyncSubject<IEnumerable<CacheElement>>)x.Item3)
                .Merge()
                .CreateCollection();
            var result = SqliteOperationQueue.CoalesceOperations(queue);

            Assert.Equal(1, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].Item1);
            Assert.Equal(2, result[0].Item2.Cast<string>().Count());

            var fakeResult = new[] {
                new CacheElement() { Key = "Foo" },
                new CacheElement() { Key = "Bar" },
            };

            var outSub = ((AsyncSubject<IEnumerable<CacheElement>>)result[0].Item3);

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
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].Item1);
            Assert.Equal(OperationType.BulkInsertSqliteOperation, result[1].Item1);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[2].Item1);
            Assert.Equal(OperationType.BulkInsertSqliteOperation, result[3].Item1);

            Assert.Equal(1, result[1].Item2.Cast<CacheElement>().First().Value[0]);
            Assert.Equal(4, result[3].Item2.Cast<CacheElement>().First().Value[0]);
        }
    }
}