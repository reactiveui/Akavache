using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Akavache.Http;
using Xunit;

namespace Akavache.Http.Tests
{
    public class CachedHttpSchedulerTests
    {
        [Fact]
        [Trait("Slow", "Very Yes")]
        public async Task OurOwnReleaseShouldBeCached()
        {
            var input = @"https://github.com/akavache/Akavache/releases/download/3.2.0/Akavache.3.2.0.zip";
            var blobCache = new TestBlobCache();
            var fixture = new CachingHttpScheduler(new HttpScheduler(), blobCache);

            fixture.Client = new HttpClient(new HttpClientHandler() {
                AllowAutoRedirect = true,
                MaxRequestContentBufferSize = 1048576 * 64,
            });

            Assert.Equal(0, blobCache.GetAllKeys().Count());
                        
            var result = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Get, new Uri(input)), 5);

            Assert.True(result.Item1.IsSuccessStatusCode);
            Assert.Equal(8089690, result.Item2.Length);
            Assert.Equal(1, blobCache.GetAllKeys().Count());

            var result2 = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Get, new Uri(input)), 3);

            Assert.True(result2.Item1.IsSuccessStatusCode);
            Assert.Equal(8089690, result2.Item2.Length);
        }
    }


}
