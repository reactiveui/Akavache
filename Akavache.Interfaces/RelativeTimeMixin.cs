using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace Akavache
{
    public static class RelativeTimeMixin
    {
        public static IObservable<Unit> Insert(this IBlobCache This, string key, byte[] data, TimeSpan expiration)
        {
            return This.Insert(key, data, This.Scheduler.Now + expiration);
        }

        public static IObservable<Unit> InsertObject<T>(this IBlobCache This, string key, T value, TimeSpan expiration)
        {
            return This.InsertObject(key, value, This.Scheduler.Now + expiration);
        }

        public static IObservable<byte[]> DownloadUrl(this IBlobCache This, string url, TimeSpan expiration, Dictionary<string, string> headers = null, bool fetchAlways = false)
        {
            return This.DownloadUrl(url, headers, fetchAlways, This.Scheduler.Now + expiration);
        }

        public static IObservable<Unit> SaveLogin(this ISecureBlobCache This, string user, string password, string host, TimeSpan expiration)
        {
            return This.SaveLogin(user, password, host, This.Scheduler.Now + expiration);
        }
    }
}