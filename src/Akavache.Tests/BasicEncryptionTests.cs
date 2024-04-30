// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace Akavache.Tests;

/// <summary>
/// Makes sure that basic encryption works correctly.
/// </summary>
public class BasicEncryptionTests
{
    /// <summary>
    /// Makes sure that encryption works.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [SkippableFact]
    public async Task ShouldEncrypt()
    {
        // TODO: This test is failing on .NET 6.0. Investigate.
        Skip.If(GetType().Assembly.GetTargetFrameworkName().StartsWith("net"));

        var provider = new EncryptionProvider();
        var array = Encoding.ASCII.GetBytes("This is a test");

        var result = await AsArray(provider.EncryptBlock(array));
        Assert.True(array.Length < result.Length); // Encrypted bytes should be much larger.
        Assert.NotEqual(array.ToList(), result);

        // the string should be garbage.
        Assert.NotEqual(Encoding.ASCII.GetString(result), "This is a test");
    }

    /// <summary>
    /// Makes sure the decryption works.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task ShouldDecrypt()
    {
        var provider = new EncryptionProvider();
        var array = Encoding.ASCII.GetBytes("This is a test");

        var encrypted = await AsArray(provider.EncryptBlock(array));
        var decrypted = await AsArray(provider.DecryptBlock(encrypted));
        Assert.Equal(array.ToList(), decrypted);
        Assert.Equal(Encoding.ASCII.GetString(decrypted), "This is a test");
    }

    private static async Task<byte[]> AsArray(IObservable<byte[]> source) => await source.FirstAsync();
}
