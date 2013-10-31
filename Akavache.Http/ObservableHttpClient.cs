using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Reactive;

namespace Akavache
{
    public static class ObservableHttpClient
    {
        /// <summary>
        /// Sends an HTTP request and attempts to cancel the request as soon as
        /// possible if requested to do so.
        /// </summary>
        /// <param name="request">The HTTP request to make</param>
        /// <param name="shouldFetchContent">If given, this predicate allows you 
        /// to cancel the request based on the returned headers. Return false to
        /// cancel reading the body</param>>
        /// <returns>A tuple of the HTTP Response and the full message 
        /// contents.</returns>
        public static IObservable<Tuple<HttpResponseMessage, byte[]>> SendAsyncObservable(this HttpClient This, HttpRequestMessage request, Func<HttpResponseMessage, bool> shouldFetchContent = null)
        {
            shouldFetchContent = shouldFetchContent ?? (_ => true);

            var cancelSignal = new AsyncSubject<Unit>();
            var ret = Observable.Create<Tuple<HttpResponseMessage, byte[]>>(async (subj, ct) => {
                try {
                    // NB: This stops HttpClient from trashing our token :-/
                    var cts = new CancellationTokenSource();
                    ct.Register(cts.Cancel);

                    var resp = await This.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                    if (!shouldFetchContent(resp)) {
                        resp.Content.Dispose();
                        cancelSignal.OnNext(Unit.Default);
                        cancelSignal.OnCompleted();
                    } else {
                        var target = new MemoryStream();
                        var source = await resp.Content.ReadAsStreamAsync();

                        await copyToAsync(source, target, ct);

                        subj.OnNext(Tuple.Create(resp, target.ToArray()));
                        subj.OnCompleted();
                    }
                } catch (Exception ex) {
                    subj.OnError(ex);
                }
            });

            return ret.TakeUntil(cancelSignal).PublishLast().RefCount();
        }

        static async Task copyToAsync(Stream source, Stream target, CancellationToken ct)
        {
            await Task.Run(async () => {
                var buf = new byte[4096];
                var read = 0;

                do {
                    read = await source.ReadAsync(buf, 0, 4096).ConfigureAwait(false);

                    if (read > 0) {
                        target.Write(buf, 0, read);
                    }
                } while (!ct.IsCancellationRequested && read > 0);

                source.Dispose();

                if (ct.IsCancellationRequested) {
                    source.Dispose();
                    target.Dispose();
                    throw new OperationCanceledException();
                }
            });
        }
    }
}