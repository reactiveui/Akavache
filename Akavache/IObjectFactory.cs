using System;

namespace Akavache
{
    public interface IObjectFactory
    {
        object Create(Type t);
    }
}
