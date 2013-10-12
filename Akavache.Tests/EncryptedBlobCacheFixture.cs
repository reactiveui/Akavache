using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using ReactiveUI.Testing;
using Xunit;
using ReactiveUI;

namespace Akavache.Tests
{
    public class EncryptedBlobCacheFixture
    {
        [Fact]
        public void NoPlaintextShouldShowUpInCache()
        {
            new TestScheduler().With(sched =>
            {
                const string secretUser = "OmgSekritUser";
                const string secretPass = "OmgSekritPassword";
                string path;

                using (Utility.WithEmptyDirectory(out path))
                {
                    using (var fixture = new TEncryptedBlobCache(path))
                    {
                        fixture.SaveLogin(secretUser, secretPass, "github.com");
                    }
                    sched.Start();

                    var di = new DirectoryInfo(path);
                    var fileList = di.GetFiles().ToArray();
                    Assert.True(fileList.Length > 1);

                    foreach (var file in fileList)
                    {
                        var text = File.ReadAllText(file.FullName, Encoding.UTF8);

                        Assert.False(text.Contains(secretUser));
                        Assert.False(text.Contains(secretPass));
                        Assert.False(text.Contains("login"));
                    }
                }
            });
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
                    fixture.SaveLogin(secretUser, secretPass, "github.com").First();
                }

                using (var fixture = new TEncryptedBlobCache(path))
                {
                    var loginInfo = fixture.GetLoginAsync("github.com").First();
                    Assert.Equal(secretUser, loginInfo.UserName);
                    Assert.Equal(secretPass, loginInfo.Password);
                }
            }
        }
    }
}
