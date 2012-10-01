using System;
using Newtonsoft.Json.Converters;

namespace Akavache
{
    public class JsonObjectConverter : CustomCreationConverter<object>
    {
        readonly IServiceProvider serviceProvider;

        public JsonObjectConverter(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");
            this.serviceProvider = serviceProvider;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsInterface;
        }

        public override object Create(Type objectType)
        {
            return serviceProvider.GetService(objectType);
        }
    }
}
