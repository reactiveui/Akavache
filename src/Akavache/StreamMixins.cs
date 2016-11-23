using System.IO;
using System.Reactive;
using System.Reactive.Subjects;

#if WINRT
using System.Reactive.Threading.Tasks;
#endif

namespace System
{
    public static class StreamMixins
    {
        public static IObservable<Unit> WriteAsyncRx(this Stream This, byte[] data, int start, int length)
        {
#if WINRT
            return This.WriteAsync(data, start, length).ToObservable();
#else
            var ret = new AsyncSubject<Unit>();

            try
            {
                This.BeginWrite(data, start, length, result =>
                {
                    try
                    {
                        This.EndWrite(result);
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