
using System.IO;
using System.Reactive;
using System.Reactive.Subjects;

#if WINDOWS_UWP
using System.Reactive.Threading.Tasks;
#endif

namespace System
{
    public static class StreamMixins
    {
        public static IObservable<Unit> WriteAsyncRx(this Stream blobCache, byte[] data, int start, int length)
        {
#if WINDOWS_UWP
            return blobCache.WriteAsync(data, start, length).ToObservable();
#else
            var ret = new AsyncSubject<Unit>();

            try
            {
                blobCache.BeginWrite(data, start, length, result =>
                {
                    try
                    {
                        blobCache.EndWrite(result);
                        ret.OnNext(Unit.Default);
                        ret.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        ret.OnError(ex);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                ret.OnError(ex);
            }

            return ret;
#endif
        }
    }
}