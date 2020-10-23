// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using ReactiveUI;

namespace Akavache.Tests
{
    /// <summary>
    /// A dummy object used in tests that replicates a routed view model.
    /// </summary>
    [DataContract]
    public class DummyRoutedViewModel : ReactiveObject, IRoutableViewModel
    {
        private Guid _aRandomGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="DummyRoutedViewModel"/> class.
        /// </summary>
        /// <param name="screen">The screen object to set.</param>
        public DummyRoutedViewModel(IScreen screen)
        {
            HostScreen = screen;
        }

        /// <summary>
        /// Gets the url path segment.
        /// </summary>
        public string UrlPathSegment => "foo";

        /// <summary>
        /// Gets the host screen.
        /// </summary>
        [DataMember]
        public IScreen HostScreen { get; private set; }

        /// <summary>
        /// Gets or sets a guid value.
        /// </summary>
        [DataMember]
        public Guid ARandomGuid
        {
            get { return _aRandomGuid; }
            set { this.RaiseAndSetIfChanged(ref _aRandomGuid, value); }
        }
    }
}
