using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.Tests.Performance
{
    public static class PerfHelper
    {
        static readonly Random prng = new Random();

        public static async Task<List<string>> GenerateDatabase(IBlobCache targetCache, int size)
        {
            var ret = new List<string>();

            // Write out in groups of 4096
            while (size > 0)
            {
                var toWriteSize = Math.Min(4096, size);
                var toWrite = GenerateRandomDatabaseContents(toWriteSize);

                await targetCache.Insert(toWrite);

                foreach (var k in toWrite.Keys) ret.Add(k);

                size -= toWrite.Count;
                Console.WriteLine(size);
            }

            return ret;
        }

        public static Dictionary<string, byte[]> GenerateRandomDatabaseContents(int toWriteSize)
        {
            var toWrite = Enumerable.Range(0, toWriteSize)
                .Select(_ => GenerateRandomKey())
                .Distinct()
                .ToDictionary(k => k, _ => GenerateRandomBytes());

            return toWrite;
        }

        public static byte[] GenerateRandomBytes()
        {
            var ret = new byte[prng.Next(1, 256)];

            prng.NextBytes(ret);
            return ret;
        }

        public static string GenerateRandomKey()
        {
            var bytes = GenerateRandomBytes();

            // NB: Mask off the MSB and set bit 5 so we always end up with
            // valid UTF-8 characters that aren't control characters
            for (int i = 0; i < bytes.Length; i++) { bytes[i] = (byte)((bytes[i] & 0x7F) | 0x20); }
            return Encoding.UTF8.GetString(bytes, 0, Math.Min(256, bytes.Length));
        }

        public static int[] GetPerfRanges()
        {
            return new[] { 1, 10, 100, 1000, 10000, 100000, };
        }
    }
}
