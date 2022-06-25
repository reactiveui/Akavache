// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Akavache;

internal abstract class KeyedOperation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedOperation"/> class.
    /// </summary>
    /// <param name="key">The key of the operation.</param>
    /// <param name="id">The ID of the operation.</param>
    protected KeyedOperation(string key, int id)
    {
        Key = key;
        Id = id;
    }

    /// <summary>
    /// Gets or sets the key for the entry.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the id for the entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets a observable which will be triggered.
    /// </summary>
    /// <returns>The observable.</returns>
    public abstract IObservable<Unit> EvaluateFunc();
}

[SuppressMessage("StyleCop.Maintainability.CSharp", "SA1402: One type per file", Justification = "Same class name.")]
internal class KeyedOperation<T> : KeyedOperation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedOperation{T}"/> class.
    /// </summary>
    /// <param name="func">The function to produce a value.</param>
    /// <param name="key">The key of the operation.</param>
    /// <param name="id">The ID of the operation.</param>
    public KeyedOperation(Func<IObservable<T>> func, string key, int id)
        : base(key, id) =>
        Func = func;

    /// <summary>
    /// Gets the function which returns the observable.
    /// </summary>
    public Func<IObservable<T>> Func { get; }

    /// <summary>
    /// Gets the result subject.
    /// </summary>
    public ReplaySubject<T> Result { get; } = new();

    /// <inheritdoc />
    public override IObservable<Unit> EvaluateFunc()
    {
        var ret = Func().Multicast(Result);
        ret.Connect();

        return ret.Select(_ => Unit.Default);
    }
}
