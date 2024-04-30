// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Serialization;

namespace Akavache.Tests;

/// <summary>
/// Tests associated with the <see cref="JsonDateTimeContractResolver"/> class.
/// </summary>
public class DateTimeResolverTests
{
    /// <summary>
    /// Checks to make sure that the JsonDateTime resolver validates correctly.
    /// </summary>
    [Fact]
    public void JsonDateTimeContractResolverValidateConverter()
    {
        // Verify our converter used
        var contractResolver = (IContractResolver)new JsonDateTimeContractResolver(null, null);
        var contract = contractResolver.ResolveContract(typeof(DateTime));
        Assert.True(contract.Converter == JsonDateTimeTickConverter.Default);
        contract = contractResolver.ResolveContract(typeof(DateTime));
        Assert.True(contract.Converter == JsonDateTimeTickConverter.Default);
        contract = contractResolver.ResolveContract(typeof(DateTime?));
        Assert.True(contract.Converter == JsonDateTimeTickConverter.Default);
        contract = contractResolver.ResolveContract(typeof(DateTime?));
        Assert.True(contract.Converter == JsonDateTimeTickConverter.Default);

        // Verify the other converter is used
        contractResolver = new JsonDateTimeContractResolver(new FakeDateTimeHighPrecisionContractResolver(), null);
        contract = contractResolver.ResolveContract(typeof(DateTime));
        Assert.True(contract.Converter is FakeDateTimeHighPrecisionJsonConverter);
        contract = contractResolver.ResolveContract(typeof(DateTimeOffset));
        Assert.True(contract.Converter == JsonDateTimeOffsetTickConverter.Default);
    }
}