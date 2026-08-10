// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;

namespace Akavache.Benchmarks;

/// <summary> Main entry point class for the Akavache V11 benchmarks. </summary>
public static class Program
{
    /// <summary> Main entry point. </summary>
    /// <param name="args">Arguments.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
