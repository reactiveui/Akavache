using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Xunit;

namespace Akavache.Tests
{
    public class UserObject
    {
        public string Bio { get; set; }
        public string Name { get; set; }
        public string Blog { get; set; }
    }

    public class BlobCacheExtensionsFixture
    {
        [Fact]
        public void DownloadUrlFact()
        {
            string path;

            using(Utility.WithEmptyDirectory(out path))
            using(var fixture = new TPersistentBlobCache(path))
            {
                var bytes = fixture.DownloadUrl(@"https://www.google.com/intl/en_com/images/srpr/logo3w.png").First();
                Assert.True(bytes.Length > 0);
            }
        }

        [Fact]
        public void ObjectsShouldBeRoundtrippable()
        {
            string path;
            var input = new UserObject() {Bio = "A totally cool cat!", Name = "octocat", Blog = "http://www.github.com"};
            UserObject result;

            using(Utility.WithEmptyDirectory(out path))
            {
                using(var fixture = new TPersistentBlobCache(path))
                {
                    fixture.InsertObject("key", input);
                }
                using(var fixture = new TPersistentBlobCache(path))
                {
                    result = fixture.GetObjectAsync<UserObject>("key").First();
                }
            }

            Assert.Equal(input.Blog, result.Blog);
            Assert.Equal(input.Bio, result.Bio);
            Assert.Equal(input.Name, result.Name);
        }
    }
}
