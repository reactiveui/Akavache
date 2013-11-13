using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Reactive.Threading.Tasks;
    
namespace Akavache.Http.Tests
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        Func<IObservable<HttpResponseMessage>> block;
        public TestHttpMessageHandler(Func<IObservable<HttpResponseMessage>> createResult)
        {
            block = createResult;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // NB: We abuse await here so that the above throw gets marshalled 
            // to a Task
            return await block().ToTask();
        }
    }
}