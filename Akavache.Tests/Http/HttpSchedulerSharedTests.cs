using Akavache.Http;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Akavache.Http.Tests
{
    public abstract class HttpSchedulerSharedTests
    {
        protected abstract IHttpScheduler CreateFixture();

        [Fact]
        public void HttpSchedulerShouldCompleteADummyRequest()
        {
            var fixture = CreateFixture();

            fixture.Client = new HttpClient(new TestHttpMessageHandler(() =>
            {
                var ret = new HttpResponseMessage()
                {
                    Content = new StringContent("foo", Encoding.UTF8),
                    StatusCode = HttpStatusCode.OK,
                };

                ret.Headers.ETag = new EntityTagHeaderValue("\"worifjw\"");
                return Observable.Return(ret);
            }));

            fixture.Client.BaseAddress = new Uri("http://example");

            var rq = new HttpRequestMessage(HttpMethod.Get, "/");
            var result = fixture.Schedule(rq, 1)
                .Timeout(TimeSpan.FromSeconds(2.0), RxApp.TaskpoolScheduler)
                .First();

            Console.WriteLine(Encoding.UTF8.GetString(result.Item2));
            Assert.Equal(HttpStatusCode.OK, result.Item1.StatusCode);
            Assert.Equal(3 /*foo*/, result.Item2.Length);
        }
    }

    public class BaseHttpSchedulerSharedTests : HttpSchedulerSharedTests
    {
        protected override IHttpScheduler CreateFixture()
        {
            return new HttpScheduler();
        }
    }

    public class CachingHttpSchedulerSharedTests : HttpSchedulerSharedTests
    {
        protected override IHttpScheduler CreateFixture()
        {
            return new CachingHttpScheduler(new TestBlobCache(), new HttpScheduler());
        }
    }
}
