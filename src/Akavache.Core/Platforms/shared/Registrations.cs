// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Splat;

#if COCOA
using Foundation;
#endif

#if ANDROID
using Android.App;
#endif

namespace Akavache.Core
{
    /// <summary>
    /// Performs registration inside the Splat DI container.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class Registrations : IWantsToRegisterStuff
    {
        /// <inheritdoc />
        public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

#if XAMARIN_MOBILE
            var fs = new IsolatedStorageProvider();
#elif WINDOWS_UWP
            var fs = new WinRTFilesystemProvider();
#else
            var fs = new SimpleFilesystemProvider();
#endif
            resolver.Register(() => fs, typeof(IFilesystemProvider), null);

#if WINDOWS_UWP
            var enc = new WinRTEncryptionProvider();
#else
            var enc = new EncryptionProvider();
#endif
            resolver.Register(() => enc, typeof(IEncryptionProvider), null);

            var localCache = new Lazy<IBlobCache>(() => new InMemoryBlobCache());
            var userAccount = new Lazy<IBlobCache>(() => new InMemoryBlobCache());
            var secure = new Lazy<ISecureBlobCache>(() => new InMemoryBlobCache());

            resolver.Register(() => localCache.Value, typeof(IBlobCache), "LocalMachine");
            resolver.Register(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
            resolver.Register(() => secure.Value, typeof(ISecureBlobCache), null);

            resolver.Register(() => new AkavacheHttpMixin(), typeof(IAkavacheHttpMixin), null);

#if COCOA
            BlobCache.ApplicationName = NSBundle.MainBundle.BundleIdentifier;
            resolver.Register(() => new MacFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif

#if ANDROID
            var ai = Application.Context.PackageManager.GetApplicationInfo(Application.Context.PackageName, 0);
            BlobCache.ApplicationName = ai.LoadLabel(Application.Context.PackageManager);

            resolver.Register(() => new AndroidFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif
        }
    }
}
