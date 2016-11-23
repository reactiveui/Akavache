using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    internal class ExceptionHelper
    {
        public static IObservable<T> ObservableThrowKeyNotFoundException<T>(string key, Exception innerException = null)
        {
            return Observable.Throw<T>(
                new KeyNotFoundException(String.Format(CultureInfo.InvariantCulture,
                "The given key '{0}' was not present in the cache.", key), innerException));
        }

        public static IObservable<T> ObservableThrowObjectDisposedException<T>(string obj, Exception innerException = null)
        {
            return Observable.Throw<T>(
                new ObjectDisposedException(String.Format(CultureInfo.InvariantCulture,
                "The cache '{0}' was disposed.", obj), innerException));
        }
    }
}
