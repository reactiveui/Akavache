// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;

namespace Akavache.Samples
{
    /// <summary>
    /// Comprehensive examples showing different patterns for using UpdateExpiration effectively.
    /// This demonstrates efficient cache expiration management without expensive data I/O operations.
    /// </summary>
    public static class UpdateExpirationPatterns
    {
        /// <summary>
        /// Pattern 1: Basic Expiration Updates - Simple single-key operations.
        /// Best for extending cache lifetime of individual items based on user activity.
        /// </summary>
        public static class BasicExpirationPattern
        {
            /// <summary>
            /// Extend cache expiration using absolute time.
            /// </summary>
            public static async Task ExtendWithAbsoluteTime()
            {
                const string cacheKey = "user_session_data";
                
                // Store some user session data
                var sessionData = new UserSession 
                { 
                    UserId = "user123", 
                    LoginTime = DateTimeOffset.Now,
                    LastActivity = DateTimeOffset.Now
                };
                
                await BlobCache.UserAccount.InsertObject(cacheKey, sessionData, 
                    DateTimeOffset.Now.AddMinutes(30));
                
                Console.WriteLine("Session data cached for 30 minutes");
                
                // Later, when user is active, extend the session
                var newExpiration = DateTimeOffset.Now.AddHours(2);
                await BlobCache.UserAccount.UpdateExpiration(cacheKey, newExpiration);
                
                Console.WriteLine($"Session extended to {newExpiration:HH:mm:ss}");
            }
            
            /// <summary>
            /// Extend cache expiration using relative time span.
            /// </summary>
            public static async Task ExtendWithRelativeTime()
            {
                const string cacheKey = "api_response_cache";
                
                // Cache an API response
                var apiData = new ApiResponse 
                { 
                    Data = "Important cached data",
                    CachedAt = DateTimeOffset.Now
                };
                
                await BlobCache.LocalMachine.InsertObject(cacheKey, apiData,
                    DateTimeOffset.Now.AddMinutes(15));
                
                Console.WriteLine("API response cached for 15 minutes");
                
                // Extend cache by another hour from now
                await BlobCache.LocalMachine.UpdateExpiration(cacheKey, TimeSpan.FromHours(1));
                
                Console.WriteLine("Cache extended by 1 hour from now");
            }
            
            /// <summary>
            /// Conditional expiration updates based on cached data inspection.
            /// </summary>
            public static async Task ConditionalExpiration()
            {
                const string cacheKey = "product_details";
                
                // Check if item exists and decide whether to extend
                try
                {
                    var product = await BlobCache.LocalMachine.GetObject<Product>(cacheKey);
                    
                    // Only extend for premium products
                    if (product.IsPremium)
                    {
                        await BlobCache.LocalMachine.UpdateExpiration(cacheKey, TimeSpan.FromDays(7));
                        Console.WriteLine($"Extended premium product cache for {product.Name}");
                    }
                    else
                    {
                        await BlobCache.LocalMachine.UpdateExpiration(cacheKey, TimeSpan.FromHours(6));
                        Console.WriteLine($"Extended standard product cache for {product.Name}");
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("Product not found in cache, nothing to extend");
                }
            }
        }

        /// <summary>
        /// Pattern 2: Bulk Expiration Updates - Efficient multi-key operations.
        /// Best for managing groups of related cache entries in a single transaction.
        /// </summary>
        public static class BulkExpirationPattern
        {
            /// <summary>
            /// Update expiration for multiple keys simultaneously.
            /// </summary>
            public static async Task BulkUpdate()
            {
                // Cache multiple user profiles
                var userKeys = new List<string>();
                for (int i = 1; i <= 5; i++)
                {
                    var key = $"user_profile_{i}";
                    var profile = new UserProfile { Id = i, Name = $"User {i}" };
                    
                    await BlobCache.UserAccount.InsertObject(key, profile, 
                        DateTimeOffset.Now.AddMinutes(30));
                    userKeys.Add(key);
                }
                
                Console.WriteLine($"Cached {userKeys.Count} user profiles");
                
                // Extend all user profiles at once (much more efficient than individual updates)
                await BlobCache.UserAccount.UpdateExpiration(userKeys, TimeSpan.FromHours(4));
                
                Console.WriteLine($"Extended expiration for {userKeys.Count} profiles in bulk");
            }
            
            /// <summary>
            /// Update expiration with type filtering for safer bulk operations.
            /// </summary>
            public static async Task BulkUpdateWithTypeFiltering()
            {
                // Cache mixed data types
                await BlobCache.LocalMachine.InsertObject("config_setting_1", "value1", DateTimeOffset.Now.AddMinutes(30));
                await BlobCache.LocalMachine.InsertObject("config_setting_2", "value2", DateTimeOffset.Now.AddMinutes(30));
                await BlobCache.LocalMachine.InsertObject("temp_data_1", 12345, DateTimeOffset.Now.AddMinutes(30));
                await BlobCache.LocalMachine.InsertObject("temp_data_2", 67890, DateTimeOffset.Now.AddMinutes(30));
                
                var keys = new[] { "config_setting_1", "config_setting_2", "temp_data_1", "temp_data_2" };
                
                // Only update string values (config settings) to have longer expiration
                await BlobCache.LocalMachine.UpdateExpiration<string>(keys, TimeSpan.FromDays(1));
                
                Console.WriteLine("Extended only string-typed cache entries");
                
                // Integer values keep their original shorter expiration
                // This prevents accidentally extending unrelated data types
            }
            
            /// <summary>
            /// Pattern-based bulk updates using key prefixes.
            /// </summary>
            public static async Task PatternBasedBulkUpdate()
            {
                // Cache session data for multiple users
                var sessionKeys = new List<string>();
                for (int i = 1; i <= 10; i++)
                {
                    var key = $"session_{i}";
                    var session = new UserSession { UserId = $"user{i}", LoginTime = DateTimeOffset.Now };
                    
                    await BlobCache.UserAccount.InsertObject(key, session, DateTimeOffset.Now.AddMinutes(30));
                    sessionKeys.Add(key);
                }
                
                // Get all session keys and extend them
                var allKeys = await BlobCache.UserAccount.GetAllKeys();
                var sessionOnlyKeys = allKeys.Where(key => key.StartsWith("session_")).ToList();
                
                await BlobCache.UserAccount.UpdateExpiration(sessionOnlyKeys, TimeSpan.FromHours(8));
                
                Console.WriteLine($"Extended {sessionOnlyKeys.Count} session entries based on key pattern");
            }
        }

        /// <summary>
        /// Pattern 3: HTTP Caching Integration - Real-world web scenarios.
        /// Best for implementing HTTP 304 Not Modified responses and conditional requests.
        /// </summary>
        public static class HttpCachingPattern
        {
            /// <summary>
            /// Handle HTTP 304 Not Modified responses by extending cache expiration.
            /// </summary>
            public static async Task Handle304NotModified()
            {
                const string cacheKey = "api_users_list";
                const string etagKey = "api_users_etag";
                
                // Simulate initial API request with data
                var users = new List<User>
                {
                    new() { Id = 1, Name = "Alice", Email = "alice@example.com" },
                    new() { Id = 2, Name = "Bob", Email = "bob@example.com" }
                };
                
                var etag = "\"abc123\"";
                
                // Cache both data and ETag
                await BlobCache.LocalMachine.InsertObject(cacheKey, users, DateTimeOffset.Now.AddMinutes(15));
                await BlobCache.LocalMachine.InsertObject(etagKey, etag, DateTimeOffset.Now.AddMinutes(15));
                
                Console.WriteLine("Cached user list with 15-minute expiration");
                
                // Later, when making a conditional request
                var cachedETag = await BlobCache.LocalMachine.GetObject<string>(etagKey);
                Console.WriteLine($"Making conditional request with ETag: {cachedETag}");
                
                // Simulate HTTP request with If-None-Match header
                var httpResponse = await SimulateConditionalHttpRequest(cachedETag);
                
                if (httpResponse.StatusCode == 304) // Not Modified
                {
                    // Extend cache expiration since server confirmed data is still fresh
                    await BlobCache.LocalMachine.UpdateExpiration(new[] { cacheKey, etagKey }, 
                        TimeSpan.FromHours(1));
                    
                    Console.WriteLine("HTTP 304: Extended cache expiration to 1 hour");
                }
                else if (httpResponse.StatusCode == 200) // Fresh data
                {
                    // Update cache with new data and ETag
                    await BlobCache.LocalMachine.InsertObject(cacheKey, httpResponse.Data, 
                        DateTimeOffset.Now.AddMinutes(15));
                    await BlobCache.LocalMachine.InsertObject(etagKey, httpResponse.ETag, 
                        DateTimeOffset.Now.AddMinutes(15));
                    
                    Console.WriteLine("HTTP 200: Updated cache with fresh data");
                }
            }
            
            /// <summary>
            /// Implement stale-while-revalidate pattern with expiration updates.
            /// </summary>
            public static async Task StaleWhileRevalidate()
            {
                const string cacheKey = "news_articles";
                
                // Check if we have cached data
                try
                {
                    var cachedArticles = await BlobCache.LocalMachine.GetObject<List<NewsArticle>>(cacheKey);
                    var cacheAge = await BlobCache.LocalMachine.GetCreatedAt(cacheKey);
                    
                    if (DateTimeOffset.Now - cacheAge > TimeSpan.FromMinutes(10))
                    {
                        Console.WriteLine("Cache is stale, serving cached data and revalidating in background");
                        
                        // Extend expiration to prevent cache eviction during background fetch
                        await BlobCache.LocalMachine.UpdateExpiration(cacheKey, TimeSpan.FromMinutes(30));
                        
                        // Serve stale data immediately
                        DisplayArticles(cachedArticles);
                        
                        // Fetch fresh data in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var freshArticles = await FetchNewsArticlesFromApi();
                                await BlobCache.LocalMachine.InsertObject(cacheKey, freshArticles,
                                    DateTimeOffset.Now.AddMinutes(20));
                                
                                Console.WriteLine("Background revalidation completed");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Background fetch failed: {ex.Message}");
                                // Keep using stale data
                            }
                        });
                    }
                    else
                    {
                        Console.WriteLine("Cache is fresh, serving cached data");
                        DisplayArticles(cachedArticles);
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("No cached data, fetching fresh data");
                    var articles = await FetchNewsArticlesFromApi();
                    await BlobCache.LocalMachine.InsertObject(cacheKey, articles,
                        DateTimeOffset.Now.AddMinutes(20));
                    DisplayArticles(articles);
                }
            }
        }

        /// <summary>
        /// Pattern 4: Session Management - User activity-based expiration.
        /// Best for maintaining user sessions with activity-based renewal.
        /// </summary>
        public static class SessionManagementPattern
        {
            /// <summary>
            /// Extend user session on activity with sliding expiration.
            /// </summary>
            public static async Task SlidingSessionExpiration()
            {
                const string sessionKey = "user_session_12345";
                
                // Create initial session
                var session = new UserSession
                {
                    UserId = "12345",
                    LoginTime = DateTimeOffset.Now,
                    LastActivity = DateTimeOffset.Now,
                    IsActive = true
                };
                
                // Cache session for 30 minutes
                await BlobCache.UserAccount.InsertObject(sessionKey, session, 
                    DateTimeOffset.Now.AddMinutes(30));
                
                Console.WriteLine("User session created with 30-minute timeout");
                
                // Simulate user activity every few minutes
                for (int i = 1; i <= 3; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1)); // Simulate time passing
                    
                    // On each user activity, extend session
                    await BlobCache.UserAccount.UpdateExpiration(sessionKey, TimeSpan.FromMinutes(30));
                    
                    Console.WriteLine($"Activity {i}: Session extended by 30 minutes from now");
                }
                
                Console.WriteLine("User session maintained through activity");
            }
            
            /// <summary>
            /// Batch session renewal for multiple users.
            /// </summary>
            public static async Task BatchSessionRenewal()
            {
                // Create multiple active sessions
                var activeSessionKeys = new List<string>();
                
                for (int i = 1; i <= 5; i++)
                {
                    var sessionKey = $"active_session_{i}";
                    var session = new UserSession 
                    { 
                        UserId = $"user{i}", 
                        LoginTime = DateTimeOffset.Now.AddMinutes(-10),
                        LastActivity = DateTimeOffset.Now.AddMinutes(-2),
                        IsActive = true
                    };
                    
                    await BlobCache.UserAccount.InsertObject(sessionKey, session,
                        DateTimeOffset.Now.AddMinutes(15)); // Will expire soon
                    
                    activeSessionKeys.Add(sessionKey);
                }
                
                Console.WriteLine($"Created {activeSessionKeys.Count} active sessions");
                
                // Batch renewal for all active sessions (much more efficient than individual updates)
                await BlobCache.UserAccount.UpdateExpiration(activeSessionKeys, TimeSpan.FromHours(2));
                
                Console.WriteLine($"Renewed {activeSessionKeys.Count} sessions in single operation");
            }
            
            /// <summary>
            /// Hierarchical session management with different timeouts.
            /// </summary>
            public static async Task HierarchicalSessionManagement()
            {
                const string userId = "premium_user_789";
                
                // Different session components with different lifetimes
                var sessionKeys = new Dictionary<string, TimeSpan>
                {
                    [$"auth_token_{userId}"] = TimeSpan.FromHours(8),      // Auth token - long lived
                    [$"csrf_token_{userId}"] = TimeSpan.FromMinutes(30),   // CSRF token - medium lived  
                    [$"temp_data_{userId}"] = TimeSpan.FromMinutes(5),     // Temporary data - short lived
                    [$"user_prefs_{userId}"] = TimeSpan.FromDays(30)       // User preferences - very long lived
                };
                
                // Cache all session components
                foreach (var kvp in sessionKeys)
                {
                    await BlobCache.UserAccount.InsertObject(kvp.Key, $"data_for_{kvp.Key}",
                        DateTimeOffset.Now.Add(kvp.Value));
                }
                
                Console.WriteLine("Created hierarchical session with different component lifetimes");
                
                // On user activity, extend different components by different amounts
                var authKeys = sessionKeys.Keys.Where(k => k.Contains("auth_token")).ToList();
                var csrfKeys = sessionKeys.Keys.Where(k => k.Contains("csrf_token")).ToList();
                
                // Extend auth tokens by their full lifetime
                await BlobCache.UserAccount.UpdateExpiration(authKeys, TimeSpan.FromHours(8));
                
                // Extend CSRF tokens by shorter amount
                await BlobCache.UserAccount.UpdateExpiration(csrfKeys, TimeSpan.FromMinutes(30));
                
                Console.WriteLine("Extended session components with appropriate lifetimes");
            }
        }

        /// <summary>
        /// Pattern 5: Performance Optimization - Efficient cache management.
        /// Best for high-performance scenarios with large numbers of cache operations.
        /// </summary>
        public static class PerformancePattern
        {
            /// <summary>
            /// Compare performance of UpdateExpiration vs traditional cache replacement.
            /// </summary>
            public static async Task PerformanceComparison()
            {
                const int itemCount = 100;
                var keys = Enumerable.Range(1, itemCount).Select(i => $"perf_test_{i}").ToList();
                
                // Setup: Cache large objects
                var largeData = new LargeDataObject 
                { 
                    Data = new string('x', 10000), // 10KB string
                    Numbers = Enumerable.Range(1, 1000).ToArray(),
                    Timestamp = DateTimeOffset.Now
                };
                
                foreach (var key in keys)
                {
                    await BlobCache.LocalMachine.InsertObject(key, largeData, 
                        DateTimeOffset.Now.AddMinutes(15));
                }
                
                Console.WriteLine($"Setup: Cached {itemCount} large objects (10KB each)");
                
                // Method 1: Traditional approach - read, modify, write
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                foreach (var key in keys.Take(10)) // Test with 10 items
                {
                    try
                    {
                        var data = await BlobCache.LocalMachine.GetObject<LargeDataObject>(key);
                        // Simulating some modification that would require re-caching
                        data.Timestamp = DateTimeOffset.Now;
                        await BlobCache.LocalMachine.InsertObject(key, data, 
                            DateTimeOffset.Now.AddHours(1));
                    }
                    catch (KeyNotFoundException)
                    {
                        // Skip missing keys
                    }
                }
                
                stopwatch.Stop();
                var traditionalTime = stopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Traditional approach (10 items): {traditionalTime}ms");
                
                // Method 2: UpdateExpiration approach
                stopwatch.Restart();
                
                await BlobCache.LocalMachine.UpdateExpiration(keys.Take(10), TimeSpan.FromHours(1));
                
                stopwatch.Stop();
                var updateExpirationTime = stopwatch.ElapsedMilliseconds;
                Console.WriteLine($"UpdateExpiration approach (10 items): {updateExpirationTime}ms");
                
                var improvement = (double)traditionalTime / updateExpirationTime;
                Console.WriteLine($"Performance improvement: {improvement:F1}x faster");
                
                // Bulk comparison
                stopwatch.Restart();
                await BlobCache.LocalMachine.UpdateExpiration(keys, TimeSpan.FromHours(1));
                stopwatch.Stop();
                
                Console.WriteLine($"Bulk UpdateExpiration ({itemCount} items): {stopwatch.ElapsedMilliseconds}ms");
            }
            
            /// <summary>
            /// Memory-efficient cache maintenance without loading data.
            /// </summary>
            public static async Task MemoryEfficientMaintenance()
            {
                // Cache many small objects
                for (int i = 1; i <= 1000; i++)
                {
                    var key = $"maintenance_item_{i}";
                    var data = new { Id = i, Name = $"Item {i}", Data = new string('x', 100) };
                    
                    await BlobCache.LocalMachine.InsertObject(key, data,
                        DateTimeOffset.Now.AddMinutes(10));
                }
                
                Console.WriteLine("Cached 1000 items for maintenance testing");
                
                // Get all keys without loading data
                var allKeys = await BlobCache.LocalMachine.GetAllKeys();
                var maintenanceKeys = allKeys.Where(k => k.StartsWith("maintenance_item_")).ToList();
                
                Console.WriteLine($"Found {maintenanceKeys.Count} items to maintain");
                
                // Extend expiration for all items without reading their data
                // This is memory-efficient as it doesn't deserialize the cached objects
                await BlobCache.LocalMachine.UpdateExpiration(maintenanceKeys, TimeSpan.FromHours(2));
                
                Console.WriteLine("Extended expiration for all items without loading data into memory");
                
                // Compare memory usage - UpdateExpiration uses minimal memory
                // while traditional approach would load all 1000 objects into memory
            }
            
            /// <summary>
            /// Conditional batch updates based on cache metadata.
            /// </summary>
            public static async Task ConditionalBatchUpdates()
            {
                var testKeys = new List<string>();
                
                // Create items with different creation times
                for (int i = 1; i <= 20; i++)
                {
                    var key = $"timed_item_{i}";
                    var data = new { Id = i, Value = $"Value {i}" };
                    
                    // Some items are "older" than others
                    var expiration = i <= 10 
                        ? DateTimeOffset.Now.AddMinutes(5)   // Will expire soon
                        : DateTimeOffset.Now.AddHours(1);    // Have time left
                    
                    await BlobCache.LocalMachine.InsertObject(key, data, expiration);
                    testKeys.Add(key);
                }
                
                Console.WriteLine("Created 20 items with mixed expiration times");
                
                // Find items that will expire soon and extend only those
                var soonToExpireKeys = new List<string>();
                
                foreach (var key in testKeys)
                {
                    try
                    {
                        var createdAt = await BlobCache.LocalMachine.GetCreatedAt(key);
                        // If created more than 2 minutes ago, it might expire soon
                        if (DateTimeOffset.Now - createdAt > TimeSpan.FromMinutes(2))
                        {
                            soonToExpireKeys.Add(key);
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        // Key doesn't exist anymore, skip
                    }
                }
                
                if (soonToExpireKeys.Any())
                {
                    await BlobCache.LocalMachine.UpdateExpiration(soonToExpireKeys, TimeSpan.FromHours(3));
                    Console.WriteLine($"Extended expiration for {soonToExpireKeys.Count} items that were expiring soon");
                }
                else
                {
                    Console.WriteLine("No items needed expiration extension");
                }
            }
        }

        /// <summary>
        /// Pattern 6: Error Handling and Best Practices.
        /// Demonstrates proper error handling and edge cases.
        /// </summary>
        public static class ErrorHandlingPattern
        {
            /// <summary>
            /// Handle common error scenarios gracefully.
            /// </summary>
            public static async Task HandleCommonErrors()
            {
                const string nonExistentKey = "does_not_exist";
                const string validKey = "valid_item";
                
                // Cache a valid item
                await BlobCache.LocalMachine.InsertObject(validKey, "test data", 
                    DateTimeOffset.Now.AddMinutes(30));
                
                // Test 1: Single key that doesn't exist
                try
                {
                    await BlobCache.LocalMachine.UpdateExpiration(nonExistentKey, TimeSpan.FromHours(1));
                    Console.WriteLine("❌ This should not happen - key doesn't exist");
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("✅ Correctly handled missing key for single update");
                }
                
                // Test 2: Bulk update with mixed existing/non-existing keys
                var mixedKeys = new[] { validKey, nonExistentKey, "another_missing_key" };
                
                try
                {
                    await BlobCache.LocalMachine.UpdateExpiration(mixedKeys, TimeSpan.FromHours(1));
                    Console.WriteLine("✅ Bulk update completed (only existing keys were updated)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bulk update handling: {ex.GetType().Name}");
                }
                
                // Test 3: Verify what was actually updated
                try
                {
                    var createdAt = await BlobCache.LocalMachine.GetCreatedAt(validKey);
                    Console.WriteLine($"✅ Valid key still exists, created at: {createdAt:HH:mm:ss}");
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("❌ Valid key was unexpectedly removed");
                }
            }
            
            /// <summary>
            /// Safe batch operations with validation.
            /// </summary>
            public static async Task SafeBatchOperations()
            {
                // Create test data
                var validKeys = new List<string>();
                for (int i = 1; i <= 5; i++)
                {
                    var key = $"safe_test_{i}";
                    await BlobCache.LocalMachine.InsertObject(key, $"data_{i}",
                        DateTimeOffset.Now.AddMinutes(15));
                    validKeys.Add(key);
                }
                
                // Mix in some invalid keys
                var keysToUpdate = validKeys.Concat(new[] { "invalid_1", "invalid_2" }).ToList();
                
                Console.WriteLine($"Attempting to update {keysToUpdate.Count} keys ({validKeys.Count} valid, {keysToUpdate.Count - validKeys.Count} invalid)");
                
                // Strategy 1: Validate keys before updating
                var existingKeys = new List<string>();
                foreach (var key in keysToUpdate)
                {
                    try
                    {
                        await BlobCache.LocalMachine.GetCreatedAt(key);
                        existingKeys.Add(key);
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine($"Skipping non-existent key: {key}");
                    }
                }
                
                // Now update only existing keys
                if (existingKeys.Any())
                {
                    await BlobCache.LocalMachine.UpdateExpiration(existingKeys, TimeSpan.FromHours(2));
                    Console.WriteLine($"✅ Successfully updated {existingKeys.Count} existing keys");
                }
                
                // Strategy 2: Use GetAllKeys to filter
                var allCacheKeys = await BlobCache.LocalMachine.GetAllKeys();
                var safeKeysToUpdate = keysToUpdate.Where(k => allCacheKeys.Contains(k)).ToList();
                
                if (safeKeysToUpdate.Any())
                {
                    await BlobCache.LocalMachine.UpdateExpiration(safeKeysToUpdate, TimeSpan.FromHours(3));
                    Console.WriteLine($"✅ Alternative approach: updated {safeKeysToUpdate.Count} verified keys");
                }
            }
            
            /// <summary>
            /// Best practices for production use.
            /// </summary>
            public static async Task ProductionBestPractices()
            {
                // Best Practice 1: Use reasonable expiration times
                const string key = "production_data";
                await BlobCache.LocalMachine.InsertObject(key, "important data", 
                    DateTimeOffset.Now.AddMinutes(30));
                
                // ✅ Good: Reasonable extension
                await BlobCache.LocalMachine.UpdateExpiration(key, TimeSpan.FromHours(4));
                Console.WriteLine("✅ Extended cache by reasonable amount (4 hours)");
                
                // ❌ Avoid: Extremely long expiration times that could cause storage issues
                // await BlobCache.LocalMachine.UpdateExpiration(key, TimeSpan.FromDays(365));
                
                // Best Practice 2: Use type filtering for safety
                var mixedTypeKeys = new[] { "string_data", "int_data", "object_data" };
                
                // Cache different types
                await BlobCache.LocalMachine.InsertObject("string_data", "text", DateTimeOffset.Now.AddMinutes(30));
                await BlobCache.LocalMachine.InsertObject("int_data", 42, DateTimeOffset.Now.AddMinutes(30));
                await BlobCache.LocalMachine.InsertObject("object_data", new { Name = "Test" }, DateTimeOffset.Now.AddMinutes(30));
                
                // ✅ Good: Type-specific updates
                await BlobCache.LocalMachine.UpdateExpiration<string>(mixedTypeKeys, TimeSpan.FromHours(2));
                Console.WriteLine("✅ Updated only string-typed entries");
                
                // Best Practice 3: Batch related operations
                var relatedKeys = new[] { "user_profile", "user_settings", "user_preferences" };
                
                // Cache related data
                foreach (var relatedKey in relatedKeys)
                {
                    await BlobCache.UserAccount.InsertObject(relatedKey, $"data_for_{relatedKey}",
                        DateTimeOffset.Now.AddMinutes(30));
                }
                
                // ✅ Good: Update related items together
                await BlobCache.UserAccount.UpdateExpiration(relatedKeys, TimeSpan.FromHours(6));
                Console.WriteLine("✅ Updated related user data together");
                
                // Best Practice 4: Monitor and log operations
                var keysToMonitor = new[] { "critical_data_1", "critical_data_2" };
                
                foreach (var monitorKey in keysToMonitor)
                {
                    await BlobCache.LocalMachine.InsertObject(monitorKey, "critical info",
                        DateTimeOffset.Now.AddMinutes(15));
                }
                
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    await BlobCache.LocalMachine.UpdateExpiration(keysToMonitor, TimeSpan.FromHours(1));
                    stopwatch.Stop();
                    
                    Console.WriteLine($"✅ Updated {keysToMonitor.Length} critical keys in {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to update critical cache keys: {ex.Message}");
                    // Implement appropriate fallback logic
                }
            }
        }

        #region Helper Methods

        private static async Task<HttpResponse> SimulateConditionalHttpRequest(string etag)
        {
            // Simulate HTTP request with conditional headers
            await Task.Delay(100); // Simulate network delay
            
            // 70% chance of 304 Not Modified, 30% chance of fresh data
            var random = new Random();
            if (random.NextDouble() < 0.7)
            {
                return new HttpResponse { StatusCode = 304 }; // Not Modified
            }
            else
            {
                return new HttpResponse 
                { 
                    StatusCode = 200,
                    Data = new List<User> { new() { Id = 3, Name = "Charlie", Email = "charlie@example.com" } },
                    ETag = "\"def456\""
                };
            }
        }

        private static async Task<List<NewsArticle>> FetchNewsArticlesFromApi()
        {
            await Task.Delay(500); // Simulate API call
            return new List<NewsArticle>
            {
                new() { Id = 1, Title = "Breaking News", PublishedAt = DateTimeOffset.Now },
                new() { Id = 2, Title = "Tech Update", PublishedAt = DateTimeOffset.Now.AddMinutes(-30) }
            };
        }

        private static void DisplayArticles(List<NewsArticle> articles)
        {
            Console.WriteLine($"Displaying {articles.Count} news articles");
            foreach (var article in articles)
            {
                Console.WriteLine($"  - {article.Title} ({article.PublishedAt:HH:mm})");
            }
        }

        #endregion

        #region Supporting Types

        public class UserSession
        {
            public string UserId { get; set; } = string.Empty;
            public DateTimeOffset LoginTime { get; set; }
            public DateTimeOffset LastActivity { get; set; }
            public bool IsActive { get; set; }
        }

        public class ApiResponse
        {
            public string Data { get; set; } = string.Empty;
            public DateTimeOffset CachedAt { get; set; }
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsPremium { get; set; }
            public decimal Price { get; set; }
        }

        public class UserProfile
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTimeOffset LastLogin { get; set; }
        }

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class NewsArticle
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTimeOffset PublishedAt { get; set; }
        }

        public class LargeDataObject
        {
            public string Data { get; set; } = string.Empty;
            public int[] Numbers { get; set; } = Array.Empty<int>();
            public DateTimeOffset Timestamp { get; set; }
        }

        public class HttpResponse
        {
            public int StatusCode { get; set; }
            public List<User>? Data { get; set; }
            public string ETag { get; set; } = string.Empty;
        }

        #endregion
    }
}