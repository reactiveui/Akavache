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

    public interface INetCache
    {
        IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority);
        HttpClient Client { get; set; }
    }

    public interface IAkavacheCachePolicy
    {
        IObservable<Tuple<HttpResponseMessage, byte[]>> RetrieveCachedResponse(HttpRequestMessage message);
        IObservable<Unit> SaveResponse(HttpResponseMessage response, byte[] body);
    }

    public class StandardNetCache : INetCache
    {
        readonly IAkavacheCachePolicy httpCache;
        readonly OperationQueue opQueue;
        readonly int priorityBase;
        readonly int retryCount;

        public StandardNetCache(IAkavacheCachePolicy httpCache, OperationQueue opQueue = null, int priorityBase = 100, int retryCount = 3)
        {
            this.httpCache = httpCache; this.opQueue = opQueue; this.priorityBase = priorityBase; this.retryCount = retryCount;
        }

        public IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority)
        {
            return httpCache.RetrieveCachedResponse(request)
                .SelectMany(resp =>
                {
                    if (resp != null) return Observable.Return(resp);
                    return scheduleDirect(request, priority);
                });
        }

        IObservable<Tuple<HttpResponseMessage, byte[]>> scheduleDirect(HttpRequestMessage request, int priority)
        {
            var rq = Observable.Defer(() => Client.SendAsyncObservable(request));
            if (retryCount > 0) 
            {
                rq = rq.Retry(retryCount);
            }

            var ret = Observable.Create<Tuple<HttpResponseMessage, byte[]>>(subj =>
            {
                var cancel = new AsyncSubject<Unit>();
                var disp = opQueue.EnqueueObservableOperation(priorityBase + priority, null, cancel, () => rq).Subscribe(subj);

                return Disposable.Create(() => 
                {
                    cancel.OnNext(Unit.Default);    
                    cancel.OnCompleted();
                    disp.Dispose();
                });
            });

            return ret.PublishLast().RefCount();
        }

        public HttpClient Client { get; set; }
    }

    public static class NetCache 
    {
        static INetCache speculative;
        [ThreadStatic] static INetCache unitTestSpeculative;
        public static INetCache Speculative
        {
            get { return unitTestSpeculative ?? speculative ?? RxApp.DependencyResolver.GetService<INetCache>("Speculative"); }
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
                
        static INetCache userInitiated;
        [ThreadStatic] static INetCache unitTestUserInitiated;
        public static INetCache UserInitiated
        {
            get { return unitTestUserInitiated ?? userInitiated ?? RxApp.DependencyResolver.GetService<INetCache>("UserInitiated"); }
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

        static INetCache background;
        [ThreadStatic] static INetCache unitTestBackground;
        public static INetCache Background
        {
            get { return unitTestBackground ?? background ?? RxApp.DependencyResolver.GetService<INetCache>("Background"); }
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
    }
}