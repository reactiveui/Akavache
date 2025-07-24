using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.Benchmarks
{
    internal static class PerfHelper
    {
        private static readonly Random _randomNumberGenerator = new Random();

        /// <summary>
        /// Tests generating a database.
        /// </summary>
        /// <param name="targetCache">The target blob cache.</param>
        /// <param name="size">The number of items to generate.</param>
        /// <returns>A list of generated items.</returns>
        public static async Task<List<string>> GenerateDatabase(IBlobCache targetCache, int size)
        {
            var ret = new List<string>();

            // Write out in groups of 4096
            while (size > 0)
            {
                var toWriteSize = Math.Min(4096, size);
                var toWrite = GenerateRandomDatabaseContents(toWriteSize);

                await targetCache.Insert(toWrite);

                foreach (var k in toWrite.Keys)
                {
                    ret.Add(k);
                }

                size -= toWrite.Count;
                Console.WriteLine(size);
            }

            return ret;
        }

        /// <summary>
        /// Generate the contents of the database.
        /// </summary>
        /// <param name="toWriteSize">The size of the database to write.</param>
        /// <returns>A dictionary of the contents.</returns>
        public static Dictionary<string, byte[]> GenerateRandomDatabaseContents(int toWriteSize)
        {
            return Enumerable.Range(0, toWriteSize)
                .Select(_ => GenerateRandomKey())
                .Distinct()
                .ToDictionary(k => k, _ => GenerateRandomBytes());
        }

        /// <summary>
        /// Generate random bytes for a value.
        /// </summary>
        /// <returns>The generated random bytes.</returns>
        public static byte[] GenerateRandomBytes()
        {
            var ret = new byte[_randomNumberGenerator.Next(1, 256)];

            _randomNumberGenerator.NextBytes(ret);
            return ret;
        }

        /// <summary>
        /// Generates a random key for the database.
        /// </summary>
        /// <returns>The random key.</returns>
        public static string GenerateRandomKey()
        {
            var bytes = GenerateRandomBytes();

            // NB: Mask off the MSB and set bit 5 so we always end up with
            // valid UTF-8 characters that aren't control characters
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((bytes[i] & 0x7F) | 0x20);
            }

            return Encoding.UTF8.GetString(bytes, 0, Math.Min(256, bytes.Length));
        }

        /// <summary>
        /// Gets a series of size values to use in generating performance tests.
        /// </summary>
        /// <returns>The range of sizes.</returns>
        public static int[] GetPerfRanges()
        {
            return new[] { 1, 10, 100, 1000, 10000, 100000, };
        }
    }
}
