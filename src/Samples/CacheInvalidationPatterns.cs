// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using Akavache.Core;
using Akavache.SystemTextJson;

namespace Akavache.Samples;

/// <summary>
/// Comprehensive examples and patterns for cache invalidation in Akavache.
/// 
/// This file demonstrates proper invalidation techniques to ensure cache consistency 
/// and prevent common issues like stale data returns after invalidation.
/// 
/// ⚠️ Important: This addresses a critical bug fixed in Akavache V11.1.1+ where 
/// calling Invalidate() on InMemory cache didn't properly clear RequestCache entries, 
/// causing GetOrFetchObject to return stale data instead of fetching fresh data.
/// </summary>
public static class CacheInvalidationPatterns
{
    /// <summary>
    /// Demonstrates the basic invalidation pattern that works correctly in V11.1.1+.
    /// 
    /// Prior to V11.1.1, this pattern would fail for InMemory cache because
    /// Invalidate() didn't clear the RequestCache, causing subsequent GetOrFetchObject
    /// calls to return stale data from the request cache.
    /// </summary>
    public static async Task BasicInvalidationPattern()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var fetchCount = 0;

        // Function that increments to simulate changing data
        Func<IObservable<string>> fetchDataFunc = () =>
        {
            fetchCount++;
            return Observable.Return($"fresh_data_{fetchCount}");
        };

        try
        {
            // Pattern 1: Initial fetch should call the function
            var firstResult = await cache.GetOrFetchObject("data_key", fetchDataFunc).FirstAsync();
            Console.WriteLine($"First fetch: {firstResult}"); // Output: fresh_data_1

            // Pattern 2: Invalidate to clear cache
            await cache.Invalidate("data_key").FirstAsync();
            
            // Pattern 3: Subsequent fetch should call function again (not return cached request)
            var secondResult = await cache.GetOrFetchObject("data_key", fetchDataFunc).FirstAsync();
            Console.WriteLine($"Second fetch after invalidation: {secondResult}"); // Output: fresh_data_2
            
            // ✅ SUCCESS: Both calls executed fetchDataFunc (fetchCount == 2)
            // ✅ SUCCESS: Different data returned after invalidation
            Console.WriteLine($"Total fetch calls: {fetchCount}"); // Output: 2
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Demonstrates bulk invalidation patterns for multiple related cache entries.
    /// Useful when you need to clear related data atomically.
    /// </summary>
    public static async Task BulkInvalidationPattern()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var callCounts = new Dictionary<string, int>();

        // Simulate multiple related data sources
        Func<string, IObservable<string>> createFetchFunc = (keyName) =>
        {
            return Observable.FromAsync(async () =>
            {
                callCounts[keyName] = callCounts.GetValueOrDefault(keyName, 0) + 1;
                // Simulate async operation
                await Task.Delay(10);
                return $"{keyName}_data_{callCounts[keyName]}";
            });
        };

        try
        {
            // Step 1: Populate cache with related data
            var userProfile = await cache.GetOrFetchObject("user_profile", 
                () => createFetchFunc("user_profile")).FirstAsync();
            var userSettings = await cache.GetOrFetchObject("user_settings", 
                () => createFetchFunc("user_settings")).FirstAsync();
            var userPermissions = await cache.GetOrFetchObject("user_permissions", 
                () => createFetchFunc("user_permissions")).FirstAsync();

            Console.WriteLine($"Initial data loaded:");
            Console.WriteLine($"  Profile: {userProfile}");
            Console.WriteLine($"  Settings: {userSettings}");
            Console.WriteLine($"  Permissions: {userPermissions}");

            // Step 2: Bulk invalidation of related user data
            var userDataKeys = new[] { "user_profile", "user_settings", "user_permissions" };
            await cache.Invalidate(userDataKeys).FirstAsync();
            Console.WriteLine("\n✅ Bulk invalidation completed");

            // Step 3: Verify fresh data is fetched (not returned from RequestCache)
            var freshProfile = await cache.GetOrFetchObject("user_profile", 
                () => createFetchFunc("user_profile")).FirstAsync();
            var freshSettings = await cache.GetOrFetchObject("user_settings", 
                () => createFetchFunc("user_settings")).FirstAsync();
            var freshPermissions = await cache.GetOrFetchObject("user_permissions", 
                () => createFetchFunc("user_permissions")).FirstAsync();

            Console.WriteLine($"\nAfter invalidation:");
            Console.WriteLine($"  Fresh Profile: {freshProfile}");
            Console.WriteLine($"  Fresh Settings: {freshSettings}");
            Console.WriteLine($"  Fresh Permissions: {freshPermissions}");

            // Verify each fetch function was called twice (initial + after invalidation)
            foreach (var kvp in callCounts)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value} calls");
                if (kvp.Value != 2)
                {
                    throw new InvalidOperationException($"Expected 2 calls for {kvp.Key}, got {kvp.Value}");
                }
            }

            Console.WriteLine("\n✅ SUCCESS: All data properly refreshed after bulk invalidation");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Demonstrates type-based invalidation patterns for cache entries of specific types.
    /// Useful for clearing all cached objects of a particular type.
    /// </summary>
    public static async Task TypeBasedInvalidationPattern()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var fetchCount = 0;

        try
        {
            // Step 1: Store different types of objects
            await cache.InsertObject("user_1", new UserData { Id = 1, Name = "Alice" });
            await cache.InsertObject("user_2", new UserData { Id = 2, Name = "Bob" });
            await cache.InsertObject("config_1", new ConfigData { Key = "theme", Value = "dark" });

            // Step 2: Setup GetOrFetchObject for a user that simulates API call
            Func<IObservable<UserData>> fetchUserFunc = () =>
            {
                fetchCount++;
                return Observable.Return(new UserData { Id = 3, Name = $"Charlie_{fetchCount}" });
            };

            // Step 3: Initial fetch and cache
            var user = await cache.GetOrFetchObject("user_3", fetchUserFunc).FirstAsync();
            Console.WriteLine($"Initial user fetch: {user.Name}"); // Charlie_1

            // Step 4: Invalidate all UserData objects
            await cache.InvalidateAllObjects<UserData>().FirstAsync();
            Console.WriteLine("✅ Invalidated all UserData objects");

            // Step 5: Verify fresh fetch occurs (RequestCache cleared)
            var freshUser = await cache.GetOrFetchObject("user_3", fetchUserFunc).FirstAsync();
            Console.WriteLine($"Fresh user fetch: {freshUser.Name}"); // Charlie_2

            // Step 6: Verify config data is still cached (different type)
            var configStillCached = await cache.GetObject<ConfigData>("config_1").FirstAsync();
            Console.WriteLine($"Config still cached: {configStillCached.Key}={configStillCached.Value}");

            // Verify fetchUserFunc was called twice
            if (fetchCount != 2)
            {
                throw new InvalidOperationException($"Expected 2 fetch calls, got {fetchCount}");
            }

            Console.WriteLine("\n✅ SUCCESS: Type-based invalidation worked correctly");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Demonstrates the common anti-pattern that would fail in versions prior to V11.1.1.
    /// 
    /// This pattern showcases the exact bug that was fixed: Invalidate() not clearing
    /// RequestCache entries for InMemory cache, causing GetOrFetchObject to return
    /// stale data from the request cache instead of fetching fresh data.
    /// </summary>
    public static async Task DemonstrateFixedBugPattern()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var apiCallCount = 0;

        // Simulate an API call that returns different data each time
        Func<IObservable<ApiResponse>> simulateApiCall = () =>
        {
            return Observable.FromAsync(async () =>
            {
                apiCallCount++;
                await Task.Delay(50); // Simulate network delay
                return new ApiResponse 
                { 
                    Data = $"API Response #{apiCallCount}",
                    Timestamp = DateTime.UtcNow,
                    Version = apiCallCount
                };
            });
        };

        try
        {
            Console.WriteLine("=== Demonstrating Fixed Bug Pattern ===");
            Console.WriteLine("Bug: Invalidate() didn't clear RequestCache for InMemory cache");
            Console.WriteLine("Fix: V11.1.1+ properly clears RequestCache on invalidation\n");

            // Step 1: Initial API call through GetOrFetchObject
            Console.WriteLine("Step 1: Initial API call...");
            var response1 = await cache.GetOrFetchObject("api_data", simulateApiCall, 
                DateTimeOffset.Now.AddMinutes(30)).FirstAsync();
            Console.WriteLine($"  Result: {response1.Data} (Version {response1.Version})");

            // Step 2: Invalidate the cache entry
            Console.WriteLine("\nStep 2: Invalidating cache entry...");
            await cache.Invalidate("api_data").FirstAsync();
            Console.WriteLine("  ✅ Cache invalidated");

            // Step 3: Call GetOrFetchObject again - should fetch fresh data
            Console.WriteLine("\nStep 3: Calling GetOrFetchObject again...");
            var response2 = await cache.GetOrFetchObject("api_data", simulateApiCall, 
                DateTimeOffset.Now.AddMinutes(30)).FirstAsync();
            Console.WriteLine($"  Result: {response2.Data} (Version {response2.Version})");

            // Verify the fix worked
            Console.WriteLine($"\nVerification:");
            Console.WriteLine($"  Total API calls: {apiCallCount}");
            Console.WriteLine($"  Response 1 version: {response1.Version}");
            Console.WriteLine($"  Response 2 version: {response2.Version}");

            if (apiCallCount == 2 && response1.Version != response2.Version)
            {
                Console.WriteLine("  ✅ SUCCESS: Bug is fixed - fresh data fetched after invalidation");
            }
            else
            {
                Console.WriteLine("  ❌ FAILURE: Bug still exists - stale data returned");
                throw new InvalidOperationException("Invalidation bug still present");
            }

            Console.WriteLine("\n📝 Note: In versions prior to V11.1.1, this test would fail");
            Console.WriteLine("   because response2 would have been the same as response1");
            Console.WriteLine("   (stale data from RequestCache instead of fresh API call)");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Demonstrates invalidation patterns for different cache types.
    /// Each cache type (UserAccount, LocalMachine, Secure, InMemory) behaves consistently.
    /// </summary>
    public static async Task CrossCacheTypeInvalidationPattern()
    {
        // Note: Using InMemoryBlobCache for all examples for simplicity
        // In real applications, you'd use BlobCache.UserAccount, BlobCache.LocalMachine, etc.
        var userCache = new InMemoryBlobCache(new SystemJsonSerializer());
        var localCache = new InMemoryBlobCache(new SystemJsonSerializer());
        var memoryCache = new InMemoryBlobCache(new SystemJsonSerializer());

        var fetchCounts = new Dictionary<string, int>();

        Func<string, IObservable<string>> createFetchFunc = (cacheType) =>
        {
            return Observable.FromAsync(async () =>
            {
                fetchCounts[cacheType] = fetchCounts.GetValueOrDefault(cacheType, 0) + 1;
                await Task.Delay(10);
                return $"{cacheType}_data_{fetchCounts[cacheType]}";
            });
        };

        try
        {
            Console.WriteLine("=== Cross-Cache Type Invalidation ===\n");

            // Test the same invalidation pattern across different cache types
            var cacheTests = new[]
            {
                (Cache: userCache, Name: "UserAccount"),
                (Cache: localCache, Name: "LocalMachine"),
                (Cache: memoryCache, Name: "InMemory")
            };

            foreach (var test in cacheTests)
            {
                Console.WriteLine($"Testing {test.Name} cache:");

                // Initial fetch
                var initial = await test.Cache.GetOrFetchObject("test_key", 
                    () => createFetchFunc(test.Name)).FirstAsync();
                Console.WriteLine($"  Initial: {initial}");

                // Invalidate
                await test.Cache.Invalidate("test_key").FirstAsync();

                // Fetch again - should get fresh data
                var fresh = await test.Cache.GetOrFetchObject("test_key", 
                    () => createFetchFunc(test.Name)).FirstAsync();
                Console.WriteLine($"  Fresh: {fresh}");

                // Verify
                var count = fetchCounts[test.Name];
                if (count == 2)
                {
                    Console.WriteLine($"  ✅ SUCCESS: {count} fetch calls as expected\n");
                }
                else
                {
                    Console.WriteLine($"  ❌ FAILURE: Expected 2 calls, got {count}\n");
                    throw new InvalidOperationException($"{test.Name} invalidation failed");
                }
            }

            Console.WriteLine("✅ All cache types handle invalidation correctly");
        }
        finally
        {
            await userCache.DisposeAsync();
            await localCache.DisposeAsync();
            await memoryCache.DisposeAsync();
        }
    }

    /// <summary>
    /// Best practices for handling invalidation in production applications.
    /// </summary>
    public static async Task ProductionInvalidationBestPractices()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());

        try
        {
            Console.WriteLine("=== Production Invalidation Best Practices ===\n");

            // Best Practice 1: Always invalidate related data together
            Console.WriteLine("1. Invalidate related data atomically:");
            await cache.InsertObject("user_profile", new { Name = "John", Email = "john@example.com" });
            await cache.InsertObject("user_settings", new { Theme = "dark", Language = "en" });
            await cache.InsertObject("user_permissions", new { Role = "admin", Permissions = new[] { "read", "write" } });

            // When user logs out, invalidate all user-related data
            var userKeys = new[] { "user_profile", "user_settings", "user_permissions" };
            await cache.Invalidate(userKeys).FirstAsync();
            Console.WriteLine("   ✅ All user data invalidated atomically");

            // Best Practice 2: Use type-based invalidation for data model changes
            Console.WriteLine("\n2. Use type-based invalidation for schema changes:");
            await cache.InsertObject("product_1", new Product { Id = 1, Name = "Widget", Price = 10.0m });
            await cache.InsertObject("product_2", new Product { Id = 2, Name = "Gadget", Price = 15.0m });
            
            // When product model changes, clear all products
            await cache.InvalidateAllObjects<Product>().FirstAsync();
            Console.WriteLine("   ✅ All Product objects invalidated");

            // Best Practice 3: Combine with error handling
            Console.WriteLine("\n3. Robust invalidation with error handling:");
            try
            {
                await cache.Invalidate("some_key").FirstAsync();
                Console.WriteLine("   ✅ Invalidation succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Invalidation failed: {ex.Message}");
                // In production: log error, use fallback strategy
            }

            // Best Practice 4: Verify invalidation worked
            Console.WriteLine("\n4. Verify invalidation effectiveness:");
            var verificationCount = 0;
            Func<IObservable<string>> verifyFunc = () =>
            {
                verificationCount++;
                return Observable.Return($"verified_{verificationCount}");
            };

            // This should trigger fetch since we invalidated above
            var result = await cache.GetOrFetchObject("verification_key", verifyFunc).FirstAsync();
            await cache.Invalidate("verification_key").FirstAsync();
            var result2 = await cache.GetOrFetchObject("verification_key", verifyFunc).FirstAsync();

            if (verificationCount == 2)
            {
                Console.WriteLine("   ✅ Invalidation verification passed");
            }
            else
            {
                Console.WriteLine($"   ❌ Invalidation verification failed: {verificationCount} calls");
            }

            Console.WriteLine("\n📋 Production Checklist:");
            Console.WriteLine("   ✅ Invalidate related data atomically");
            Console.WriteLine("   ✅ Use type-based invalidation for schema changes");
            Console.WriteLine("   ✅ Include error handling around invalidation");
            Console.WriteLine("   ✅ Verify invalidation effectiveness in tests");
            Console.WriteLine("   ✅ Document invalidation triggers in your code");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Sample data classes for demonstration purposes.
    /// </summary>
    public class UserData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ConfigData
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ApiResponse
    {
        public string Data { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Version { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}