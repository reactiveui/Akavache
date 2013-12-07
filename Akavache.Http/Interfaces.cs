using System;
using System.Net.Http;
using ReactiveUI;
using Punchclock;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Disposables;

namespace Akavache.Http
{
    public enum Priorities {
        Speculative = 10,
        UserInitiated = 100,
        Background = 20,
        BackgroundGuaranteed = 30,
    }

    public interface IHttpScheduler
    {
        IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority);
        void ResetLimit(long? maxBytesToRead = null);
        void CancelAll();

        HttpClient Client { get; set; }
    }

    public static class NetCache 
    {
        static IHttpScheduler speculative;
        [ThreadStatic] static IHttpScheduler unitTestSpeculative;
        public static IHttpScheduler Speculative
        {
            get { return unitTestSpeculative ?? speculative ?? RxApp.DependencyResolver.GetService<IHttpScheduler>("Speculative"); }
            set 
            {
                if (RxApp.InUnitTestRunner())
                {
                    unitTestSpeculative = value;
                    speculative = speculative ?? value;
                }
                else
                {
                    speculative = value;
                }
            }
        }
                
        static IHttpScheduler userInitiated;
        [ThreadStatic] static IHttpScheduler unitTestUserInitiated;
        public static IHttpScheduler UserInitiated
        {
            get { return unitTestUserInitiated ?? userInitiated ?? RxApp.DependencyResolver.GetService<IHttpScheduler>("UserInitiated"); }
            set 
            {
                if (RxApp.InUnitTestRunner())
                {
                    unitTestUserInitiated = value;
                    userInitiated = userInitiated ?? value;
                }
                else
                {
                    userInitiated = value;
                }
            }
        }

        static IHttpScheduler background;
        [ThreadStatic] static IHttpScheduler unitTestBackground;
        public static IHttpScheduler Background
        {
            get { return unitTestBackground ?? background ?? RxApp.DependencyResolver.GetService<IHttpScheduler>("Background"); }
            set 
            {
                if (RxApp.InUnitTestRunner())
                {
                    unitTestBackground = value;
                    background = background ?? value;
                }
                else
                {
                    background = value;
                }
            }
        }

        static IHttpScheduler backgroundGuaranteed;
        [ThreadStatic] static IHttpScheduler unitTestBackgroundGuaranteed;
        public static IHttpScheduler BackgroundGuaranteed
        {
            get { return unitTestBackgroundGuaranteed ?? backgroundGuaranteed ?? RxApp.DependencyResolver.GetService<IHttpScheduler>("BackgroundGuaranteed"); }
            set 
            {
                if (RxApp.InUnitTestRunner())
                {
                    unitTestBackgroundGuaranteed = value;
                    backgroundGuaranteed = backgroundGuaranteed ?? value;
                }
                else
                {
                    backgroundGuaranteed = value;
                }
            }
        }
    }
}