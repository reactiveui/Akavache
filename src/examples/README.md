# Akavache examples

The order cache saves an order with source-generated JSON, retrieves its key and creation time, extends its expiry, then invalidates it. The executable checks the restored order and the final contents of the cache with JSON reflection disabled.

The [encrypted order archive](OrderCache/OrderArchive.cs) writes an order to SQLite, closes the connection and reads it back after reopening the file. Both sessions use the same encryption key. The BSON-compatible serializer's metadata overload writes ordinary JSON bytes. The runner generates a temporary key for this demonstration and removes the database when it finishes.

The [dispatch manifest](OrderCache/DispatchManifest.cs) uses Newtonsoft.Json at runtime. It writes a batch of orders in BSON to a plain SQLite database. A second session reads the cached order, publishes a revision with `GetAndFetchLatest`, and removes the dispatched order. The integration tests check both versions and the order left in the database.

The [delivery instructions](OrderCache/DeliveryInstructions.cs) use a per-cache HTTP service and an expiring key. After the dispatch service changes the note, a cached read still returns “Leave with reception.” A forced refresh returns “Signature required.” The HTTP handler runs locally.

In an app with a Splat bitmap loader, the [delivery photo](OrderCache/DeliveryPhoto.cs) saves the original image and creates a 96-pixel preview under its own key. The integration tests run this example with an in-memory bitmap loader for both package families.

The [delivery preferences](OrderCache/DeliveryPreferences.cs) expose a live notification setting using the runtime serializer. From `src`, run
`dotnet test --project tests/Akavache.Integration.Tests/Akavache.Integration.Tests.csproj -- --treenode-filter "/*/*/OrderCacheExampleTests/DeliveryPreferencesPublishesSavedChoice"`
to see the check for a subscriber receiving the dispatcher's saved choice.

From `src`, run the project for your package family.

| Package | Command |
| --- | --- |
| Akavache.SystemTextJson | `dotnet run --project examples/OrderCache -f net11.0` |
| Akavache.SystemTextJson.Reactive | `dotnet run --project examples/OrderCache.Reactive -f net11.0` |

Both examples also target .NET 10. The [desktop and mobile samples](../samples/README.md) show how to use a persistent cache in a WPF or MAUI app.

For other update policies, see the [cached and fresh results](../samples/GetAndFetchLatestPatterns.cs), [expiration](../samples/UpdateExpirationPatterns.cs) and [invalidation](../samples/CacheInvalidationPatterns.cs) examples.
