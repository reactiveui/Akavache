using Newtonsoft.Json;
using ReactiveUI.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache.Mobile
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type, string> registerFunction)
        {
            registerFunction(() => new JsonSerializerSettings() 
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            }, typeof(JsonSerializerSettings), null);

            var akavacheDriver = new AkavacheDriver();
            registerFunction(() => akavacheDriver, typeof(ISuspensionDriver), null);
        }
    }
}
