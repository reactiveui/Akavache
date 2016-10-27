### 5.0.0

**Breaking Changes**

- Added support for Android 7 by changing SQLite dependency to SQLitePCLRaw.bundle_e_sqlite3.
- Retired all 32bit platforms (Windows Phone 8, MonoMac, MonoTouch, Xamarin.[iOS|Mac] (32bit/non-unified).
- Retired Windows RT80, since VS2015 does not support it anymore.
- Retired Xamarin component store; please use NuGet.org instead.
- Pinned the Splat dependency in the nuspec file to the same version we build Akavache with.


**Bug Fixes**

- Resolved infinite self-recursion defect with the InsertAllObjects method.

**Features**

- Performance improvements by indexing the Typename and Expiration.
- Performance improvements by lazily initializing various SQLite operations.
- Implemented continuous integration.
- Compile Xamarin.Mac project on Windows.
- Restructured project layout (on filesystem).
- Added Code of Conduct.
