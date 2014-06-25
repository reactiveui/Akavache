using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Splat;

namespace Akavache
{
    internal abstract class KeyedOperation
    {
        public string Key { get; set; }
        public int Id { get; set; }
        public abstract IObservable<Unit> EvaluateFunc();
    }

    internal class KeyedOperation<T> : KeyedOperation
    {
        public Func<IObservable<T>> Func { get; set; }
        public readonly ReplaySubject<T> Result = new ReplaySubject<T>();

        public override IObservable<Unit> EvaluateFunc()
        {
            var ret = Func().Multicast(Result);
            ret.Connect();

            return ret.Select(_ => Unit.Default);
        }
    }

    public class KeyedOperationQueue : IKeyedOperationQueue, IEnableLogger
    {
        readonly IScheduler scheduler;
        static int sequenceNumber = 1;
        readonly Subject<KeyedOperation> queuedOps = new Subject<KeyedOperation>();
        readonly IConnectableObservable<KeyedOperation> resultObs;
        AsyncSubject<Unit> shutdownObs;

        public KeyedOperationQueue(IScheduler scheduler = null)
        {
            scheduler = scheduler ?? BlobCache.TaskpoolScheduler;
            this.scheduler = scheduler;
            resultObs = queuedOps
                .GroupBy(x => x.Key)
                .Select(x => x.Select(ProcessOperation).Concat())
                .Merge()
                .Multicast(new Subject<KeyedOperation>());

            resultObs.Connect();
        }

        /// <summary>
        ///   Queue an operation to run in the background. All operations with the same key will run in sequence,
        ///   waiting for the previous operation to complete.
        /// </summary>
        /// <param name = "key">The key to use</param>
        /// <param name = "action">A method to run in the background</param>
        /// <returns>A future representing when the operation completes</returns>
        public IObservable<Unit> EnqueueOperation(string key, Action action)
        {
            return EnqueueOperation(key, () =>
            {
                action();
                return Unit.Default;
            });
        }

        /// <summary>
        ///   Queue an operation to run in the background that returns a value. All operations with the same key will run in sequence,
        ///   waiting for the previous operation to complete.
        /// </summary>
        /// <param name="key">The key to use</param>
        /// <param name="calculationFunc">A method to run in the background that returns a single value</param>
        /// <returns>A future value</returns>
        public IObservable<T> EnqueueOperation<T>(string key, Func<T> calculationFunc)
        {
            return EnqueueObservableOperation(key, () => SafeStart(calculationFunc));
        }

        /// <summary>
        ///   Queue an operation to run in the background that returns a stream of values. All operations with the same key will run in sequence,
        ///   waiting for the previous operation to complete.
        ///   If you want to queue an operation that already returns IObservable, this is your guy.
        /// </summary>
        /// <param name="key">The key to use</param>
        /// <param name="asyncCalculationFunc">A method to run in the background that returns a stream of values</param>
        /// <returns>A future stream of values</returns>
        public IObservable<T> EnqueueObservableOperation<T>(string key, Func<IObservable<T>> asyncCalculationFunc)
        {
            int id = Interlocked.Increment(ref sequenceNumber);
            key = key ?? "__NONE__";

            this.Log().Debug("Queuing operation {0} with key {1}", id, key);
            var item = new KeyedOperation<T>
            {
                Key = key, Id = id,
                Func = asyncCalculationFunc,
            };

            queuedOps.OnNext(item);
            return item.Result;
        }

        public IObservable<Unit> ShutdownQueue()
        {
            lock (queuedOps)
            {
                if (shutdownObs != null) return shutdownObs;

                queuedOps.OnCompleted();

                shutdownObs = new AsyncSubject<Unit>();
                var sub = resultObs.Materialize()
                    .Where(x => x.Kind != NotificationKind.OnNext)
                    .SelectMany(x =>
                        (x.Kind == NotificationKind.OnError) ?
                            Observable.Throw<Unit>(x.Exception) :
                            Observable.Return(Unit.Default))
                    .Multicast(shutdownObs);

                sub.Connect();

                return shutdownObs;
            }
        }

        static IObservable<KeyedOperation> ProcessOperation(KeyedOperation operation)
        {
            return Observable.Defer(operation.EvaluateFunc)
                .Select(_ => operation)
                .Catch(Observable.Return(operation));
        }

        IObservable<T> SafeStart<T>(Func<T> calculationFunc)
        {
            var ret = new AsyncSubject<T>();
            Observable.Start(() =>
            {
                try
                {
                    var val = calculationFunc();
                    ret.OnNext(val);
                    ret.OnCompleted();
                }
                catch (Exception ex)
                {
                    this.Log().WarnException("Failure running queued op", ex);
                    ret.OnError(ex);
                }
            }, scheduler);

            return ret;
        }
    }
}
