using System;

namespace Akavache
{
    public interface IObjectCreator : IServiceProvider
    {
        bool CanCreate(Type type);
    }
}
