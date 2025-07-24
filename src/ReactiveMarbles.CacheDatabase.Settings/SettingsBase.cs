// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Settings.Core;
#if ENCRYPTED
using ReactiveMarbles.CacheDatabase.EncryptedSettings;
#endif

namespace ReactiveMarbles.CacheDatabase.Settings
{
    /// <summary>
    /// Empty Base.
    /// </summary>
    public abstract class SettingsBase : SettingsStorage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsBase"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        protected SettingsBase(string className)
            : base($"__{className}__", AppInfo.BlobCaches[className]!)
        {
        }
    }
}
