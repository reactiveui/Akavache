using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache.Sqlite3;
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

            var result = SqliteOperationQueue.CoalesceOperations(fixture.DumpQueue());

            Assert.Equal(1, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].Item1);
        }

        [Fact]
        public void UnrelatedItems()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });
            fixture.Insert(new[] { new CacheElement() { Key = "Bar" } });
            fixture.Invalidate(new[] { "Baz" });

            var result = SqliteOperationQueue.CoalesceOperations(fixture.DumpQueue());

            Assert.Equal(3, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].Item1);
        }

        [Fact]
        public void CoalesceUnrelatedSelects()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });
            fixture.Select(new[] { "Bar" });
            fixture.Invalidate(new[] { "Bamf" });
            fixture.Select(new[] { "Baz" });

            var result = SqliteOperationQueue.CoalesceOperations(fixture.DumpQueue());

            Assert.Equal(2, result.Count);

            var item = result.Single(x => x.Item1 == OperationType.BulkSelectSqliteOperation);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, item.Item1);
            Assert.Equal(3, item.Item2.Cast<string>().Count());
        }

        [Fact]
        public void DedupRelatedSelects()
        {
            var fixture = new SqliteOperationQueue();
            fixture.Select(new[] { "Foo" });
            fixture.Select(new[] { "Foo" });
            fixture.Select(new[] { "Bar" });
            fixture.Select(new[] { "Foo" });

            var result = SqliteOperationQueue.CoalesceOperations(fixture.DumpQueue());

            Assert.Equal(2, result.Count);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].Item1);
            Assert.Equal(OperationType.BulkSelectSqliteOperation, result[1].Item1);
            Assert.Equal(1, result[0].Item2.Cast<string>().Count());
            Assert.Equal(1, result[0].Item2.Cast<string>().Count());
        }
    }
}
