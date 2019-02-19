// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
        private readonly IContractResolver _existingContractResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDateTimeContractResolver"/> class.
        /// </summary>
        /// <param name="contractResolver">A inherited contract resolver.</param>
        public JsonDateTimeContractResolver(IContractResolver contractResolver)
        {
            _existingContractResolver = contractResolver;
        }

        /// <inheritdoc />
        public override JsonContract ResolveContract(Type type)
        {
            var contract = _existingContractResolver?.ResolveContract(type);
            if (contract?.Converter != null)
            {
                return contract;
            }

            if (contract == null)
            {
                contract = base.ResolveContract(type);
            }

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
