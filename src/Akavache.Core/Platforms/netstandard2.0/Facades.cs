using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;

namespace Akavache
{
    public static class Utility
    {
        public static string GetMd5Hash(string input)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> LogErrors<T>(this IObservable<T> This, string message = null)
        {
            throw new NotImplementedException();
        }

        public static IObservable<Unit> CopyToAsync(this Stream This, Stream destination, IScheduler scheduler = null)
        {
            throw new NotImplementedException();
        }
    }
}
