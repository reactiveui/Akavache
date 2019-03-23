// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Splat;

namespace Akavache
{
    /// <summary>
    /// A key which has separate buckets for each key.
    /// </summary>
    public class KeyedOperationQueue : IKeyedOperationQueue, IEnableLogger, IDisposable
    {
        private static int _sequenceNumber = 1;
        private readonly IScheduler _scheduler;
        private readonly Subject<KeyedOperation> _queuedOps = new Subject<KeyedOperation>();
        private readonly IConnectableObservable<KeyedOperation> _resultObs;
        private AsyncSubject<Unit> _shutdownObs;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyedOperationQueue"/> class.
        /// </summary>
        /// <param name="scheduler">The scheduler for Observable operations.</param>
        public KeyedOperationQueue(IScheduler scheduler = null)
        {
            scheduler = scheduler ?? BlobCache.TaskpoolScheduler;
            _scheduler = scheduler;
            _resultObs = _queuedOps
                .GroupBy(x => x.Key)
                .Select(x => x.Select(ProcessOperation).Concat())
                .Merge()
                .Multicast(new Subject<KeyedOperation>());

            _resultObs.Connect();
        }

        /// <summary>
        ///   Queue an operation to run in the background. All operations with the same key will run in sequence,
        ///   waiting for the previous operation to complete.
        /// </summary>
        /// <param name = "key">The key to use.</param>
        /// <param name = "action">A method to run in the background.</param>
        /// <returns>A future representing when the operation completes.</returns>
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
        /// <param name="key">The key to use.</param>
        /// <param name="calculationFunc">A method to run in the background that returns a single value.</param>
        /// <typeparam name="T">The type of item in the queue.</typeparam>
        /// <returns>A future value.</returns>
        public IObservable<T> EnqueueOperation<T>(string key, Func<T> calculationFunc)
        {
            return EnqueueObservableOperation(key, () => SafeStart(calculationFunc));
        }

        /// <summary>
        ///   Queue an operation to run in the background that returns a stream of values. All operations with the same key will run in sequence,
        ///   waiting for the previous operation to complete.
        ///   If you want to queue an operation that already returns IObservable, this is your guy.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <param name="asyncCalculationFunc">A method to run in the background that returns a stream of values.</param>
        /// <typeparam name="T">The type of value in the queue.</typeparam>
        /// <returns>A future stream of values.</returns>
        public IObservable<T> EnqueueObservableOperation<T>(string key, Func<IObservable<T>> asyncCalculationFunc)
        {
            int id = Interlocked.Increment(ref _sequenceNumber);
            key = key ?? "__NONE__";

            this.Log().Debug(CultureInfo.InvariantCulture, "Queuing operation {0} with key {1}", id, key);
            var item = new KeyedOperation<T>
            {
                Key = key, Id = id,
                Func = asyncCalculationFunc,
            };

            _queuedOps.OnNext(item);
            return item.Result;
        }

        /// <summary>
        /// Shuts the queue and stops it from processing.
        /// </summary>
        /// <returns>An observable that signals when the shutdown is complete.</returns>
        public IObservable<Unit> ShutdownQueue()
        {
            lock (_queuedOps)
            {
                if (_shutdownObs != null)
                {
                    return _shutdownObs;
                }

                _queuedOps.OnCompleted();

                _shutdownObs = new AsyncSubject<Unit>();
                var sub = _resultObs.Materialize()
                    .Where(x => x.Kind != NotificationKind.OnNext)
                    .SelectMany(x =>
                        (x.Kind == NotificationKind.OnError) ?
                            Observable.Throw<Unit>(x.Exception) :
                            Observable.Return(Unit.Default))
                    .Multicast(_shutdownObs);

                sub.Connect();

                return _shutdownObs;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of managed memory for the class.
        /// </summary>
        /// <param name="disposing">If this method is being called by the Dispose method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _queuedOps?.Dispose();
                _shutdownObs?.Dispose();
            }
        }

        private static IObservable<KeyedOperation> ProcessOperation(KeyedOperation operation)
        {
            return Observable.Defer(operation.EvaluateFunc)
                .Select(_ => operation)
                .Catch(Observable.Return(operation));
        }

        private IObservable<T> SafeStart<T>(Func<T> calculationFunc)
        {
            var ret = new AsyncSubject<T>();
            Observable.Start(
                () =>
            {
                try
                {
                    var val = calculationFunc();
                    ret.OnNext(val);
                    ret.OnCompleted();
                }
                catch (Exception ex)
                {
                    this.Log().Warn(ex, "Failure running queued op");
                    ret.OnError(ex);
                }
            }, _scheduler);

            return ret;
        }
    }
}
