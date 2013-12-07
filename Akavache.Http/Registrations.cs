using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using Newtonsoft.Json;
using ReactiveUI.Mobile;
using ReactiveUI;
using System.Reactive.Disposables;

#if UIKIT
using MonoTouch.SystemConfiguration;
#endif

namespace Akavache.Http
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type, string> registerFunction)
        {
            var background = new Lazy<IHttpScheduler>(() =>
            {
                var suspHost = RxApp.DependencyResolver.GetService<ISuspensionHost>();
                var ret = new CachingHttpScheduler(new HttpScheduler((int)Priorities.Background, 1));

                if (suspHost != null)
                {
                    suspHost.ShouldPersistState.Subscribe(_ => ret.CancelAll());
                }
                return ret;
            });
            registerFunction(() => background.Value, typeof(IHttpScheduler), "Background");

            var userInitiated = new Lazy<IHttpScheduler>(() =>
            {
                var suspHost = RxApp.DependencyResolver.GetService<ISuspensionHost>();
                var ret = new CachingHttpScheduler(new HttpScheduler((int)Priorities.UserInitiated, 3));

                if (suspHost != null)
                {
                    suspHost.ShouldPersistState.Subscribe(_ => ret.CancelAll());
                }
                return ret;
            });
            registerFunction(() => userInitiated.Value, typeof(IHttpScheduler), "UserInitiated");

            var speculative = new Lazy<IHttpScheduler>(() =>
            {
                var suspHost = RxApp.DependencyResolver.GetService<ISuspensionHost>();
                var ret = new CachingHttpScheduler(new HttpScheduler((int)Priorities.Speculative, 0));
                ret.ResetLimit(GetDataLimit());

                if (suspHost != null)
                {
                    suspHost.ShouldPersistState.Subscribe(_ => ret.CancelAll());
                    suspHost.IsUnpausing.Subscribe(_ => ret.ResetLimit(GetDataLimit()));
                }

                return ret;
            });
            registerFunction(() => speculative.Value, typeof(IHttpScheduler), "Speculative");
        }

#if PORTABLE
        static long GetDataLimit()
        {
            return 5 * 1048576;
        }
#endif

#if NET45
        static long GetDataLimit()
        {
            return 10 * 1048576;
        }
#endif

#if UIKIT
        static long GetDataLimit()
        {
            var nm = new NetworkReachability("google.com");
            var flags = default(NetworkReachabilityFlags);

            if (!nm.TryGetFlags(out flags)) {
                return 512 * 1024;
            }

            if (flags & NetworkReachabilityFlags.IsWWAN) {
                return 5 * 1048576;
            }

            return 10 * 1048576;
        }
#endif
    }
}