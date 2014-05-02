using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Akavache.Sqlite3
{
    sealed class AsyncReaderWriterLock
    {
        bool isShutdown;
        readonly TaskFactory readScheduler;
        readonly TaskFactory writeScheduler;

        public AsyncReaderWriterLock()
        {
            var pair = new ConcurrentExclusiveSchedulerPair();

            readScheduler = new TaskFactory(
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskContinuationOptions.None, pair.ConcurrentScheduler);
            writeScheduler = new TaskFactory(
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskContinuationOptions.None, pair.ExclusiveScheduler);
        }

        public IObservable<IDisposable> AcquireRead()
        {
            return AcquireOnScheduler(readScheduler);
        }

        public IObservable<IDisposable> AcquireWrite()
        {
            return AcquireOnScheduler(writeScheduler);
        }

        AsyncSubject<Unit> shutdownResult;
        public IObservable<Unit> Shutdown()
        {
            if (shutdownResult != null) return shutdownResult;

            shutdownResult = new AsyncSubject<Unit>();

            // NB: Just grab the write lock to shut down
            var writeFuture = AcquireWrite();
            isShutdown = true;

            var ret = writeFuture
                .Select(x => { x.Dispose(); return Unit.Default; })
                .Multicast(shutdownResult);
            ret.Connect();

            return ret;
        }

        IObservable<IDisposable> AcquireOnScheduler(TaskFactory sched)
        {
            if (isShutdown) return Observable.Throw<IDisposable>(new ObjectDisposedException("AsyncReaderWriterLock"));

            var ret = new AsyncSubject<IDisposable>();
            var gate = new AsyncSubject<Unit>();

            sched.StartNew(() =>
            {
                // NB: At this point we know that we are currently executing on the
                // scheduler (i.e. if this was the exclusive scheduler, we know that
                // all the readers have been thrown out)
                var disp = Disposable.Create(() => { gate.OnNext(Unit.Default); gate.OnCompleted(); });
                ret.OnNext(disp);
                ret.OnCompleted();

                // Trashing the returned Disposable will unlock this gate
                // NB: Why not await? Holding this task alive will ensure that 
                // we don't release the exclusive half of the pair. If we await, 
                // the task exits until the gate is signalled. That means that
                // the scheduler is free when it shouldn't be.
                gate.Wait();
            });

            return ret;
        }
    }
}
