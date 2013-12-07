using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
    
namespace Akavache.Http.Tests
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        Func<HttpRequestMessage, IObservable<HttpResponseMessage>> block;
        public TestHttpMessageHandler(Func<HttpRequestMessage, IObservable<HttpResponseMessage>> createResult)
        {
            block = createResult;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Observable.Throw<HttpResponseMessage>(new OperationCanceledException()).ToTask();
            }

            return block(request).ToTask(cancellationToken);
        }
    }
}