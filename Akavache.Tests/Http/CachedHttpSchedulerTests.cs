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
using System.IO;
using Akavache.Tests;

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

            Assert.Equal(0, blobCache.GetAllKeys().First().Count());
                        
            var result = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Get, new Uri(input)), 5);

            Assert.True(result.Item1.IsSuccessStatusCode);
            Assert.Equal(8089690, result.Item2.Length);
            Assert.Equal(1, blobCache.GetAllKeys().First().Count());

            var result2 = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Get, new Uri(input)), 3);

            Assert.True(result2.Item1.IsSuccessStatusCode);
            Assert.Equal(8089690, result2.Item2.Length);
        }

        [Fact]
        [Trait("Slow", "Very Yes")]
        public async Task FailedRequestsShouldntBeCached()
        {
            var input = @"https://httpbin.org/status/502";
            var blobCache = new TestBlobCache();
            var fixture = new CachingHttpScheduler(new HttpScheduler(), blobCache);

            fixture.Client = new HttpClient(new HttpClientHandler() {
                AllowAutoRedirect = true,
                MaxRequestContentBufferSize = 1048576 * 64,
            });

            Assert.Equal(0, blobCache.GetAllKeys().First().Count());

            var result = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Get, new Uri(input)), 5);

            Assert.Equal(0, blobCache.GetAllKeys().First().Count());
            Assert.False(result.Item1.IsSuccessStatusCode);
        }

        [Fact]
        [Trait("Slow", "Very Yes")]
        public async Task PostsShouldntBeCached()
        {
            var input = @"https://httpbin.org/post";
            var blobCache = new TestBlobCache();
            var fixture = new CachingHttpScheduler(new HttpScheduler(), blobCache);

            fixture.Client = new HttpClient(new HttpClientHandler() {
                AllowAutoRedirect = true,
                MaxRequestContentBufferSize = 1048576 * 64,
            });

            Assert.Equal(0, blobCache.GetAllKeys().First().Count());

            var result = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Post, new Uri(input)), 5);

            Assert.Equal(0, blobCache.GetAllKeys().First().Count());
            Assert.True(result.Item1.IsSuccessStatusCode);
        }

        [Fact]
        public async Task PostsWithETagsShouldBeCached()
        {
            var input = @"https://httpbin.org/post";
            var blobCache = new TestBlobCache();
            var fixture = new CachingHttpScheduler(new HttpScheduler(), blobCache);
            var requestCount = 0;

            fixture.Client = new HttpClient(new TestHttpMessageHandler(_ => {
                var response = IntegrationTestHelper.GetResponse("Http", "fixtures", "ResponseWithETag");
                requestCount++;

                if (requestCount > 1)
                {
                    // Rig the data to be zero - if we see this, we know we didn't
                    // use the cached version
                    var newData = new ByteArrayContent(new byte[0]);
                    foreach (var kvp in response.Content.Headers) newData.Headers.Add(kvp.Key, kvp.Value);
                    response.Content = newData;
                }

                return Observable.Return(response);
            }));

            Assert.Equal(0, blobCache.GetAllKeys().First().Count());
            Assert.Equal(0, requestCount);

            var result = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Get, new Uri(input)), 5);

            Assert.Equal(1, blobCache.GetAllKeys().First().Count());
            Assert.Equal(1, requestCount);
            Assert.True(result.Item2.Length > 0);

            var result2 = await fixture.Schedule(new HttpRequestMessage(HttpMethod.Get, new Uri(input)), 5);

            Assert.Equal(1, blobCache.GetAllKeys().First().Count());
            Assert.Equal(2, requestCount);
            Assert.True(result2.Item2.Length > 0);
        }
    }
}