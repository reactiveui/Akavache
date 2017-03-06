using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public class InMemoryBlobCacheProvider : IBlobCacheProvider
    {
        public IBlobCache CreateLocalMachine(string fileName)
        {
            return new InMemoryBlobCache();
        }

        public ISecureBlobCache CreateSecure(string fileName)
        {
            return new InMemoryBlobCache();
        }

        public IBlobCache CreateUserAccount(string fileName)
        {
            return new InMemoryBlobCache();
        }
    }
}
