using System;
using System.Reflection;
using Newtonsoft.Json.Converters;

namespace Akavache
{
    public class JsonObjectConverter : CustomCreationConverter<object>
    {
        readonly IObjectFactory objectFactory;

        public JsonObjectConverter(IObjectFactory objectFactory)
        {
            if (objectFactory == null) throw new ArgumentNullException("objectFactory");
            this.objectFactory = objectFactory;
        }

        public override bool CanConvert(Type objectType)
        {
#if NETFX_CORE
            return objectType.GetTypeInfo().IsInterface;
#else
            return objectType.IsInterface;
#endif
        }

        public override object Create(Type objectType)
        {
            return objectFactory.Create(objectType);
        }
    }
}
