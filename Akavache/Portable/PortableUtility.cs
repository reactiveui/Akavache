using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    internal static class PortableExtensions
    {
        public static T Retry<T>(this Func<T> block, int retries = 3)
        {
            while (true)
            {
                try
                {
                    T ret = block();
                    return ret;
                }
                catch (Exception)
                {
                    retries--;
                    if (retries == 0)
                    {
                        throw;
                    }
                }
            }
        }

        internal static IObservable<T> PermaRef<T>(this IConnectableObservable<T> This)
        {
            This.Connect();
            return This;
        }
    }
}
