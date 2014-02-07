using System;
using System.Net.Http;
using Punchclock;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Disposables;
using Splat;

namespace Akavache.Http
{
    /// <summary>
    /// This enumeration defines the default base priorities associated with the
    /// different NetCache instances
    /// </summary>
    public enum Priorities {
        Speculative = 10,
        UserInitiated = 100,
        Background = 20,
        //BackgroundGuaranteed = 30,
    }

    /// <summary>
    /// This interface defines a cache for HTTP Requests, it is the
    /// Akavache.Http analog to IBlobCache.
    /// </summary>
    public interface IHttpScheduler
    {
        /// <summary>
        /// Schedules an HTTP request to be sent and returns the result along
        /// with the content.
        /// </summary>
        /// <param name="request">The HTTP request to schedule.</param>
        /// <param name="priority">The absolute priority to schedule this
        /// request. Higher priorities get scheduled before lower
        /// priorities.</param>
        /// <param name="shouldFetchContent">This Func is called when the
        /// Headers of the HTTP request are first recieved. If Func returns
        /// false, the operation should be cancelled and the body should not
        /// be returned.</param>
        /// <returns>A Future result representing the HTTP response as well as
        /// the body of the message.</returns>
        IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority, Func<HttpResponseMessage, bool> shouldFetchContent);

        /// <summary>
        /// Cancel all outstanding requests that are in-flight.
        /// </summary>
        void CancelAll();

        /// <summary>
        /// The HttpClient instance to use to send requests.
        /// </summary>
        HttpClient Client { get; set; }
    }

    /// <summary>
    /// Speculative HTTP schedulers only allow a certain number of bytes to be
    /// read before cancelling all future requests. This is designed for
    /// reading data that may or may not be used by the user later, in order
    /// to improve response times should the user later request the data.
    /// </summary>
    public interface ISpeculativeHttpScheduler : IHttpScheduler
    {
        /// <summary>
        /// Resets the total limit of bytes to read. This is usually called
        /// when the app resumes from suspend, to indicate that we should
        /// fetch another set of data.
        /// </summary>
        /// <param name="maxBytesToRead"></param>
        void ResetLimit(long? maxBytesToRead = null);
    }

    public static class NetCache 
    {
        static ISpeculativeHttpScheduler speculative;
        [ThreadStatic] static ISpeculativeHttpScheduler unitTestSpeculative;

        /// <summary>
        /// Speculative HTTP schedulers only allow a certain number of bytes to be
        /// read before cancelling all future requests. This is designed for
        /// reading data that may or may not be used by the user later, in order
        /// to improve response times should the user later request the data.
        /// </summary>
        public static ISpeculativeHttpScheduler Speculative
        {
            get { return unitTestSpeculative ?? speculative ?? Locator.Current.GetService<ISpeculativeHttpScheduler>("Speculative"); }
            set 
            {
                if (ModeDetector.InUnitTestRunner())
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

        /// <summary>
        /// This scheduler should be used for requests initiated by a user
        /// action such as clicking an item, they have the highest priority.
        /// </summary>
        public static IHttpScheduler UserInitiated
        {
            get { return unitTestUserInitiated ?? userInitiated ?? Locator.Current.GetService<IHttpScheduler>("UserInitiated"); }
            set 
            {
                if (ModeDetector.InUnitTestRunner())
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

        /// <summary>
        /// This scheduler should be used for requests initiated in the
        /// background, and are scheduled at a lower priority.
        /// </summary>
        public static IHttpScheduler Background
        {
            get { return unitTestBackground ?? background ?? Locator.Current.GetService<IHttpScheduler>("Background"); }
            set 
            {
                if (ModeDetector.InUnitTestRunner())
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

        /*
        static IHttpScheduler backgroundGuaranteed;
        [ThreadStatic] static IHttpScheduler unitTestBackgroundGuaranteed;
        public static IHttpScheduler BackgroundGuaranteed
        {
            get { return unitTestBackgroundGuaranteed ?? backgroundGuaranteed ?? Locator.Current.GetService<IHttpScheduler>("BackgroundGuaranteed"); }
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
        */
    }
}
