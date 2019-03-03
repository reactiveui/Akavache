// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Akavache
{
    internal abstract class KeyedOperation
    {
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
        /// Gets or sets the function which returns the observable.
        /// </summary>
        public Func<IObservable<T>> Func { get; set; }

        /// <summary>
        /// Gets the result subject.
        /// </summary>
        public ReplaySubject<T> Result { get; } = new ReplaySubject<T>();

        /// <inheritdoc />
        public override IObservable<Unit> EvaluateFunc()
        {
            var ret = Func().Multicast(Result);
            ret.Connect();

            return ret.Select(_ => Unit.Default);
        }
    }
}