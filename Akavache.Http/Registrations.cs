using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReactiveUI.Mobile;

namespace Akavache.Http
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type, string> registerFunction)
        {
            var background = new CachingHttpScheduler(new HttpScheduler((int)Priorities.Background, 1));
            registerFunction(() => background, typeof(IHttpScheduler), "Background");

            var userInitiated = new CachingHttpScheduler(new HttpScheduler((int)Priorities.UserInitiated, 3));
            registerFunction(() => userInitiated, typeof(IHttpScheduler), "UserInitiated");

            var speculative = new CachingHttpScheduler(new HttpScheduler((int)Priorities.Speculative, 0));
            speculative.ResetLimit(5 * 1048576);
            registerFunction(() => speculative, typeof(IHttpScheduler), "Speculative");
        }
    }
}