using System.IO;
using System.Reactive;
using System.Reactive.Subjects;

namespace System
{
    public static class StreamMixins
    {
#if NETFX_CORE
        public static IObservable<Unit> WriteAsyncRx(this Stream This, byte[] data, int start, int length)
        {
            throw new Exception("yeah yeah its coming");
        }
#else
        public static IObservable<Unit> WriteAsyncRx(this Stream This, byte[] data, int start, int length)
        {
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
        }
#endif
    }
}