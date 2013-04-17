using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache.Internal
{
    internal interface IAdapterResolver
    {
        object Resolve(Type type);
    }
}
