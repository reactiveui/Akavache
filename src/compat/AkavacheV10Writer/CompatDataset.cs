// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace AkavacheV10Writer;

/// <summary>
/// The deterministic entries this writer stores. The V11 reader carries the same set, and the two
/// copies together are the contract the compatibility check tests: a value written by Akavache 10
/// must still read back through Akavache 11.
/// </summary>
internal static class CompatDataset
{
    /// <summary>Key of the plain string entry.</summary>
    internal const string StringKey = "compat:string";

    /// <summary>Key of the boxed integer entry.</summary>
    internal const string IntKey = "compat:int";

    /// <summary>Key of the serialized object entry.</summary>
    internal const string PersonKey = "compat:person";

    /// <summary>Key of the raw byte-array entry.</summary>
    internal const string BytesKey = "compat:bytes";

    /// <summary>Value stored under <see cref="StringKey"/>.</summary>
    internal const string StringValue = "Hello, Akavache V10!";

    /// <summary>Value stored under <see cref="IntKey"/>.</summary>
    internal const int IntValue = 42;

    /// <summary>Age carried by the person stored under <see cref="PersonKey"/>.</summary>
    internal const int PersonAge = 36;

    /// <summary>Name carried by the person stored under <see cref="PersonKey"/>.</summary>
    internal const string PersonName = "Ada Lovelace";

    /// <summary>Email carried by the person stored under <see cref="PersonKey"/>.</summary>
    internal const string PersonEmail = "ada@example.com";

    /// <summary>Gets the value stored under <see cref="PersonKey"/>.</summary>
    internal static Person PersonValue => new() { Name = PersonName, Age = PersonAge, Email = PersonEmail };

    /// <summary>Creates the value stored under <see cref="BytesKey"/>.</summary>
    /// <returns>The payload bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] CreateBytesValue() => "ByteArray:CAFEBABE"u8.ToArray();
}
