using Newtonsoft.Json;
using ReactiveUI.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace Akavache.Mobile
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(IMutableDependencyResolver resolver)
        {
            resolver.Register(() => new JsonSerializerSettings() 
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            }, typeof(JsonSerializerSettings), null);

            var akavacheDriver = new AkavacheDriver();
            resolver.Register(() => akavacheDriver, typeof(ISuspensionDriver), null);
        }
    }
}