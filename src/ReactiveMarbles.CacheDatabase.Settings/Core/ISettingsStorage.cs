// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace ReactiveMarbles.CacheDatabase.Settings.Core
{
    /// <summary>
    /// Interface for SettingsStorage.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="System.IAsyncDisposable" />
    public interface ISettingsStorage : INotifyPropertyChanged, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Loads every setting in this storage into the internal cache, or, if the value doesn't
        /// exist in the storage, initializes it with its default value. You dont HAVE to call this
        /// method, but it's handy for applications with a high number of settings where you want to
        /// load all settings on startup at once into the internal cache and not one-by-one at each request.
        /// </summary>
        /// <returns>A Task.</returns>
        Task InitializeAsync();
    }
}
