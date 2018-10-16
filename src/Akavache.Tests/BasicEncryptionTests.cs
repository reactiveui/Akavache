using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Akavache.Tests
{
    public class BasicEncryptionTests
    {
        [Fact]
        public async Task ShouldEncrypt()
        {
            var provider = new EncryptionProvider();
            var array = Encoding.ASCII.GetBytes("This is a test");

            var result = await AsArray(provider.EncryptBlock(array));
            Assert.True(array.Length < result.Length); // Encrypted bytes should be much larger 
            Assert.NotEqual(array.ToList(), result);
            //the string should be garbage.
            Assert.NotEqual(Encoding.ASCII.GetString(result), "This is a test");
        }

        [Fact]
        public async Task ShouldDecrypt()
        {
            var provider = new EncryptionProvider();
            var array = Encoding.ASCII.GetBytes("This is a test");

            var encrypted = await AsArray(provider.EncryptBlock(array));
            var decrypted = await AsArray(provider.DecryptBlock(encrypted));
            Assert.Equal(array.ToList(), decrypted);
            Assert.Equal(Encoding.ASCII.GetString(decrypted), "This is a test");
        }

        private async Task<byte[]> AsArray(IObservable<byte[]> source)
        {
            return await source.FirstAsync();
        }
    }
}
