using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akavache.Tests
{
    /// <summary>
    /// Used for various tests that stress Akavache
    /// </summary>
    internal class Stresser
    {
        static readonly Random random = new Random(DateTime.UtcNow.Millisecond);
        readonly List<Task> tasks = new List<Task>();
        readonly ConcurrentBag<Exception> exceptions = new ConcurrentBag<Exception>();
        bool isRunning;

        public Stresser(IEnumerable<Action<string>> actions) : this(actions, 1)
        {
        }

        public Stresser(IEnumerable<Action<string>> actions, int uniqueKeyCount)
        {
            tasks = (
                from action in actions
                from counter in Enumerable.Range(0, uniqueKeyCount)
                let key = Guid.NewGuid().ToString()
                select new Task(() => RunAction(key, action)))
                .ToList();
        }

        public ConcurrentBag<Exception> RunActions(TimeSpan timeout)
        {
            isRunning = true;
            tasks.ForEach(t => t.Start());
            var timeoutDate = DateTime.UtcNow.Add(timeout);

            while (isRunning && DateTime.UtcNow <= timeoutDate)
            {
                Thread.Sleep(0);
            }
            isRunning = false;
            //tasks.ForEach(t => t.Join());
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            return exceptions;
        }

        public static byte[] RandomData()
        {
            return Enumerable.Range(0, 10).SelectMany(_ => BitConverter.GetBytes(random.Next())).ToArray();
        }

        void RunAction(string key, Action<string> action)
        {
            try
            {
                while (isRunning)
                {
                    try
                    {
                        action(key);
                    }
                    catch (KeyNotFoundException)
                    {
                        // continue. This is to be expected.
                    }
                }
            }
            catch (Exception e)
            {
                isRunning = false;
                exceptions.Add(e);
            }
        }
    }
}
