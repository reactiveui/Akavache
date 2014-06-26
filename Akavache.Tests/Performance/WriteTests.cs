using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache.Sqlite3;
using Xunit;

namespace Akavache.Tests.Performance
{
    public abstract class WriteTests
    {
        protected abstract IBlobCache CreateBlobCache(string path);
        readonly Random prng = new Random();

        [Fact]
        public async Task SequentialSimpleWrites()
        {
        }

        [Fact]
        public async Task SequentialBulkWrites()
        {
        }

        [Fact]
        public async Task ParallelSimpleWrites()
        { 
        }

        public async Task GeneratePerfRangesForBlock(Func<IBlobCache, int, Task<long>> block)
        {
            var results = new Dictionary<int, long>();
            var dbName = default(string);

            var dirPath = default(string);
            using (Utility.WithEmptyDirectory(out dirPath))
            using (var cache = CreateBlobCache(dirPath))
            {
                dbName = dbName ?? cache.GetType().Name;

                foreach (var size in PerfHelper.GetPerfRanges())
                {
                    results[size] = await block(cache, size);
                }
            }

            Console.WriteLine(dbName);
            foreach (var kvp in results) {
                Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
            }
        }
    }

    public class Sqlite3ReadTests : WriteTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new SqlitePersistentBlobCache(Path.Combine(path, "blob.db"));
        }
    }
}
