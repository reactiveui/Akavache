using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Akavache.Tests
{
    public class KeyedMonitorTests
    {
        const int milliseconds = 300;

        [Fact]
        public void SerializesAccessToAKey()
        {
            var monitor = new KeyedMonitor();
            int key1Counter = -1;
            var counters = new List<int>();
            var stresser = new Stresser(new Action<string>[]
            {
                key => monitor.Try(key, () => counters.Add(++key1Counter)),
                key => monitor.Try(key, () => counters.Add(++key1Counter)),
                key => monitor.Try(key, () => counters.Add(++key1Counter))
            }, uniqueKeyCount: 1);

            var exceptions = stresser.RunActions(TimeSpan.FromMilliseconds(milliseconds));

            Assert.Empty(exceptions);
            for (int i = 0; i < counters.Count; i++)
            {
                if (i == counters[i]) continue;
                Assert.Equal("", "Race condition at " + i);
            }
        }

        [Fact]
        public void AllowsAccessToDifferentKeysInParallel()
        {
            var monitor = new KeyedMonitor();
            var thread1Counters = new List<int>();
            var thread2Counters = new List<int>();
            Func<string, List<int>> counters = k => k.StartsWith("1:") ? thread1Counters : thread2Counters;
            int counter = -1;
            // Two threads trying to access same counter. Since they're in parallel, 
            // we should see a shared value.
            var stresser = new Stresser(new Action<string>[]
            {
                key => monitor.Try(key, () => counters(key).Add(++counter)),
                key => monitor.Try(key, () => counters(key).Add(++counter)),
                key => monitor.Try(key, () => counters(key).Add(++counter))
            }, uniqueKeyCount: 2);

            var exceptions = stresser.RunActions(TimeSpan.FromMilliseconds(milliseconds));

            Assert.Empty(exceptions);
            Assert.NotEmpty(thread1Counters.Intersect(thread2Counters));
        }

        [Fact]
        public void SerializesAccessPerKeyForMulitpleKeys()
        {
            var monitor = new KeyedMonitor();
            var thread1Counters = new List<int>();
            var thread2Counters = new List<int>();
            Func<string, List<int>> counters = k => k.StartsWith("1:") ? thread1Counters : thread2Counters;
            int thread1Counter = -1;
            int thread2Counter = -1;
            Func<string, int> increment = k => 
                k.StartsWith("1:") ? ++thread1Counter : ++thread2Counter;
            // Two threads trying to access same counter. Since they're in parallel, 
            // we should see a shared value.
            var stresser = new Stresser(new Action<string>[]
            {
                key => monitor.Try(key, () => counters(key).Add(increment(key))),
                key => monitor.Try(key, () => counters(key).Add(increment(key))),
                key => monitor.Try(key, () => counters(key).Add(increment(key)))
            }, uniqueKeyCount: 2);

            var exceptions = stresser.RunActions(TimeSpan.FromMilliseconds(milliseconds));

            Assert.Empty(exceptions);
            string failures = "";
            for (int i = 0; i < thread1Counters.Count; i++)
            {
                if (i == thread1Counters[i]) continue;
                failures = "Thread1 got out of sync at " + i + Environment.NewLine;
                break;
            }
            for (int i = 0; i < thread2Counters.Count; i++)
            {
                if (i == thread2Counters[i]) continue;
                failures += "Thread2 got out of sync at " + i;
                break;
            }
            Assert.Equal("", failures);
        }
    }
}
