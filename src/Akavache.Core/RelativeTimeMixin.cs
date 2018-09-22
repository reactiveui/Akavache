using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace Akavache
{
    /// <summary>
    /// Relative Time Mixin
    /// </summary>
    public static class RelativeTimeMixin
    {
        /// <summary>
        /// Inserts the specified key.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        public static IObservable<Unit> Insert(this IBlobCache This, string key, byte[] data, TimeSpan expiration)
        {
            return This.Insert(key, data, This.Scheduler.Now + expiration);
        }

        /// <summary>
        /// Inserts the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This">The this.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        public static IObservable<Unit> InsertObject<T>(this IBlobCache This, string key, T value, TimeSpan expiration)
        {
            return This.InsertObject(key, value, This.Scheduler.Now + expiration);
        }

        /// <summary>
        /// Downloads the URL.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="url">The URL.</param>
        /// <param name="expiration">The expiration.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="fetchAlways">if set to <c>true</c> [fetch always].</param>
        /// <returns></returns>
        public static IObservable<byte[]> DownloadUrl(this IBlobCache This, string url, TimeSpan expiration, Dictionary<string, string> headers = null, bool fetchAlways = false)
        {
            return This.DownloadUrl(url, headers, fetchAlways, This.Scheduler.Now + expiration);
        }

        /// <summary>
        /// Saves the login.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="host">The host.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        public static IObservable<Unit> SaveLogin(this ISecureBlobCache This, string user, string password, string host, TimeSpan expiration)
        {
            return This.SaveLogin(user, password, host, This.Scheduler.Now + expiration);
        }
    }
}
