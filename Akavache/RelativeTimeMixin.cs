using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

#if SILVERLIGHT
using System.Net.Browser;
#endif

namespace Akavache
{
    public static class RelativeTimeMixin
    {
        public static void Insert(this IBlobCache This, string key, byte[] data, TimeSpan expiration)
        {
            This.Insert(key, data, This.Scheduler.Now + expiration);
        }

        public static void InsertObject<T>(this IBlobCache This, string key, T value, TimeSpan expiration)
        {
            This.InsertObject(key, value, This.Scheduler.Now + expiration);
        }

        public static IObservable<byte[]> DownloadUrl(this IBlobCache This, string url, TimeSpan expiration, Dictionary<string, string> headers = null, bool fetchAlways = false)
        {
            return This.DownloadUrl(url, headers, fetchAlways, This.Scheduler.Now + expiration);
        }

        public static void SaveLogin(this ISecureBlobCache This, string user, string password, string host, TimeSpan expiration)
        {
            This.SaveLogin(user, password, host, This.Scheduler.Now + expiration);
        }
    }
}
