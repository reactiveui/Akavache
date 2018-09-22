using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public static class Utility
    {
        /// <summary>
        /// Copies to asynchronous.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IObservable<Unit> CopyToAsync(this Stream This, Stream destination, IScheduler scheduler = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the MD5 hash.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string GetMd5Hash(string input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Logs the errors.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This">The this.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IObservable<T> LogErrors<T>(this IObservable<T> This, string message = null)
        {
            throw new NotImplementedException();
        }
    }
}
