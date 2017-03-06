using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public interface IBlobCacheProvider
    {
        IBlobCache CreateLocalMachine(string fileName);
        IBlobCache CreateUserAccount(string fileName);
        ISecureBlobCache CreateSecure(string fileName);
    }
}
