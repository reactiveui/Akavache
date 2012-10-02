using System;
using Newtonsoft.Json.Converters;

namespace Akavache
{
    public class JsonObjectConverter : CustomCreationConverter<object>
    {
        readonly IServiceProvider serviceProvider;
        readonly IObjectCreator objectCreator;

        public JsonObjectConverter(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");

            this.serviceProvider = serviceProvider;
            objectCreator = serviceProvider as IObjectCreator;
        }

        public override bool CanConvert(Type objectType)
        {
            // If the service provider isn't also an object creator,
            // it had better be able to create any type.
            return objectCreator == null || objectCreator.CanCreate(objectType);
        }

        public override object Create(Type objectType)
        {
            return serviceProvider.GetService(objectType);
        }
    }
}
