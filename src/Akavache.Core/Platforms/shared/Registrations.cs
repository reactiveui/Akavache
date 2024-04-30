// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

#if COCOA
using Foundation;
#endif

#if ANDROID
using Android.App;
#if ANDROID33_0_OR_GREATER
using System.Runtime.Versioning;
#endif
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
#if ANDROID && ANDROID33_0_OR_GREATER
        [ObsoletedOSPlatform("android33.0")]
#endif
        public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
        {
#if NETSTANDARD || XAMARINIOS || XAMARINMAC || XAMARINTVOS || TIZEN || MONOANDROID13_0
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }
#else
            ArgumentNullException.ThrowIfNull(resolver);
#endif

#if XAMARIN_MOBILE
            var fs = new IsolatedStorageProvider();
#else
            var fs = new SimpleFilesystemProvider();
#endif
            resolver.Register(() => fs, typeof(IFilesystemProvider), null);

            var enc = new EncryptionProvider();
            resolver.Register(() => enc, typeof(IEncryptionProvider), null);

            var localCache = new Lazy<IBlobCache>(() => new InMemoryBlobCache());
            var userAccount = new Lazy<IBlobCache>(() => new InMemoryBlobCache());
            var secure = new Lazy<ISecureBlobCache>(() => new InMemoryBlobCache());

            resolver.Register(() => localCache.Value, typeof(IBlobCache), "LocalMachine");
            resolver.Register(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
            resolver.Register(() => secure.Value, typeof(ISecureBlobCache), null);

            resolver.Register(() => new AkavacheHttpMixin(), typeof(IAkavacheHttpMixin), null);
            resolver.Register(() => new DefaultAkavacheHttpClientFactory(), typeof(IAkavacheHttpClientFactory), null);

#if COCOA
            BlobCache.ApplicationName = NSBundle.MainBundle.BundleIdentifier;
            resolver.Register(() => new MacFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif

#if ANDROID

            var packageManager = Application.Context.PackageManager;
            var packageName = Application.Context.PackageName ?? "Unknown Package";
            var applicationLabel = packageManager?.GetApplicationInfo(packageName, 0)?.LoadLabel(packageManager);

            BlobCache.ApplicationName = applicationLabel ?? "Unknown";

            resolver.Register(() => new AndroidFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif
        }
    }
}
