using System;
using System.Reactive;
using System.Reactive.Linq;
using Newtonsoft.Json;
using ReactiveUI;
using Splat;

namespace Akavache.Mobile
{
    public class AkavacheDriver : ISuspensionDriver, IEnableLogger
    {
        public IObservable<object> LoadState()
        {
            return BlobCache.UserAccount.GetObject<object>("__AppState");
        }

        public IObservable<Unit> SaveState(object state)
        {
            return BlobCache.UserAccount.InsertObject("__AppState", state)
                .SelectMany(BlobCache.UserAccount.Flush());
        }

        public IObservable<Unit> InvalidateState()
        {
            return BlobCache.UserAccount.InvalidateObject<object>("__AppState");
        }
    }
}
