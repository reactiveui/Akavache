using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace Akavache
{
    public interface IAkavacheHttpMixin
    {
        IObservable<byte[]> DownloadUrl(IBlobCache This, string url, IDictionary<string, string> headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);
        IObservable<byte[]> DownloadUrl(IBlobCache This, string key, string url, IDictionary<string, string> headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);
    }
}
