using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using Xunit;
using ReactiveUI;

namespace Akavache.Tests
{
    class TEncryptedBlobCache : EncryptedBlobCache
    {
        public TEncryptedBlobCache (string cacheDirectory = null, IScheduler scheduler = null) : base(cacheDirectory, null, scheduler) { }
    }

    public class EncryptedBlobCacheFixture : IEnableLogger
    {
        [Fact]
        public void NoPlaintextShouldShowUpInCache()
        {
            const string secretUser = "OmgSekritUser";
            const string secretPass = "OmgSekritPassword";
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = new TEncryptedBlobCache(path))
                {
                    fixture.SaveLogin(secretUser, secretPass);
                }

                var di = new DirectoryInfo(path);
                var fileList = di.GetFiles().ToArray();
                Assert.True(fileList.Length > 1);

                foreach(var file in fileList)
                {
                    var text = File.ReadAllText(file.FullName, Encoding.UTF8);
                    this.Log().InfoFormat("File '{0}': {1}", file.Name, text);

                    Assert.False(text.Contains(secretUser));
                    Assert.False(text.Contains(secretPass));
                    Assert.False(text.Contains("login"));
                }
            }
        }

        [Fact]
        public void EncryptedDataShouldBeRoundtripped()
        {
            const string secretUser = "OmgSekritUser";
            const string secretPass = "OmgSekritPassword";
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = new TEncryptedBlobCache(path))
                {
                    fixture.SaveLogin(secretUser, secretPass);
                }

                using (var fixture = new TEncryptedBlobCache(path))
                {
                    var loginInfo = fixture.GetLoginAsync().First();
                    Assert.Equal(secretUser, loginInfo.Item1);
                    Assert.Equal(secretPass, loginInfo.Item2);
                }
            }
        }
    }
}
