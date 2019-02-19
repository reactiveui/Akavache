using System;
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

        public override JsonContract ResolveContract(Type type)
        {
            var contract = existingContractResolver?.ResolveContract(type);
            if (contract?.Converter != null)
                return contract;

            if (contract == null)
                contract = base.ResolveContract(type);

            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                contract.Converter = JsonDateTimeTickConverter.Default;
            }
            else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                contract.Converter = JsonDateTimeOffsetTickConverter.Default;
            }

            return contract;
        }
    }
}
