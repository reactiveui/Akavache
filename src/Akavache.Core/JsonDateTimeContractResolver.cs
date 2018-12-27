using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace Akavache
{
    /// <summary>
    /// Resolver which will handle DateTime and DateTimeOffset with our own internal resolver.
    /// It will also be able to use, if set, a external provider that a user has set.
    /// </summary>
    internal class JsonDateTimeContractResolver : DefaultContractResolver
    {
        private readonly IContractResolver existingContractResolver;

        public JsonDateTimeContractResolver(IContractResolver contractResolver)
        {
            existingContractResolver = contractResolver;
        }

        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = existingContractResolver?.ResolveContract(objectType) ?? base.CreateContract(objectType);
            if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
            {
                contract.Converter = JsonDateTimeTickConverter.Default;
            }

            if (objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?))
            {
                contract.Converter = JsonDateTimeOffsetTickConverter.Default;
            }

            return contract;
        }
    }
}
