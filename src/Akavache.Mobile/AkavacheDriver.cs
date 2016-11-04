using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Splat;
using Newtonsoft.Json;

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
