// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Akavache;

namespace Akavache.Samples
{
    /// <summary>
    /// Comprehensive examples showing different patterns for using GetAndFetchLatest effectively.
    /// This addresses the common question: "What's the right way to handle GetAndFetchLatest?"
    /// </summary>
    public static class GetAndFetchLatestPatterns
    {
        /// <summary>
        /// Pattern 1: Simple Replacement - Most common and straightforward pattern.
        /// Best for scenarios where you want to completely replace UI content.
        /// </summary>
        public static class SimpleReplacementPattern
        {
            public static void Example()
            {
                // This is the recommended pattern for most scenarios
                CacheDatabase.LocalMachine.GetAndFetchLatest("user_profile",
                    () => FetchUserProfileFromApi())
                    .Subscribe(userProfile => 
                    {
                        // This subscriber will be called twice:
                        // 1. Immediately with cached data (if available)
                        // 2. When fresh data arrives from the API
                        
                        // Simply update the UI each time - works perfectly for both calls
                        DisplayUserProfile(userProfile);
                        
                        Console.WriteLine($"User profile updated: {userProfile.Name}");
                    });
            }

            private static IObservable<UserProfile> FetchUserProfileFromApi()
            {
                // Simulate API call
                return Observable.FromAsync(async () =>
                {
                    await Task.Delay(1000); // Simulate network delay
                    return new UserProfile 
                    { 
                        Name = "John Doe", 
                        Email = "john@example.com",
                        LastUpdated = DateTimeOffset.Now
                    };
                });
            }

            private static void DisplayUserProfile(UserProfile profile)
            {
                // Update UI - this method should handle being called multiple times
                Console.WriteLine($"Displaying profile: {profile.Name} ({profile.Email})");
            }
        }

        /// <summary>
        /// Pattern 2: Merge Strategy for Collections - Smart merging of new data with existing.
        /// Best for lists where you want to add new items without losing existing ones.
        /// </summary>
        public static class MergeStrategyPattern
        {
            public class MessageService
            {
                private readonly List<Message> _currentMessages = new();
                private bool _isFirstLoad = true;

                public IObservable<List<Message>> GetMessages(int ticketId)
                {
                    return CacheDatabase.LocalMachine.GetAndFetchLatest($"messages_{ticketId}",
                        () => FetchMessagesFromApi(ticketId))
                        .Do(messages => ProcessMessageUpdate(messages))
                        .Select(_ => _currentMessages.ToList()); // Return defensive copy
                }

                private void ProcessMessageUpdate(List<Message> incomingMessages)
                {
                    if (_isFirstLoad)
                    {
                        // First call: cached data or initial fresh data
                        _currentMessages.Clear();
                        _currentMessages.AddRange(incomingMessages);
                        _isFirstLoad = false;
                        
                        Console.WriteLine($"Loaded {incomingMessages.Count} messages from cache/initial fetch");
                    }
                    else
                    {
                        // Second call: merge fresh data with existing
                        var newMessages = incomingMessages
                            .Where(msg => !_currentMessages.Any(existing => existing.Id == msg.Id))
                            .ToList();
                        
                        if (newMessages.Any())
                        {
                            _currentMessages.AddRange(newMessages);
                            
                            // Sort by timestamp to maintain order
                            _currentMessages.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                            
                            Console.WriteLine($"Merged {newMessages.Count} new messages");
                        }
                        else
                        {
                            Console.WriteLine("No new messages to merge");
                        }
                    }
                }
            }

            private static IObservable<List<Message>> FetchMessagesFromApi(int ticketId)
            {
                return Observable.FromAsync(async () =>
                {
                    await Task.Delay(800); // Simulate API call
                    return new List<Message>
                    {
                        new() { Id = 1, Content = "Hello", Timestamp = DateTimeOffset.Now.AddMinutes(-30) },
                        new() { Id = 2, Content = "How are you?", Timestamp = DateTimeOffset.Now.AddMinutes(-20) },
                        new() { Id = 3, Content = "New message", Timestamp = DateTimeOffset.Now }
                    };
                });
            }
        }

        /// <summary>
        /// Pattern 3: Differential Updates with State Tracking - Maximum control over data merging.
        /// Best for complex scenarios requiring fine-grained update logic.
        /// </summary>
        public static class DifferentialUpdatesPattern
        {
            public class NewsService
            {
                private readonly Subject<NewsUpdateEvent> _updateEvents = new();
                private readonly List<NewsItem> _currentNews = new();
                private bool _hasCachedData = false;

                public IObservable<List<NewsItem>> GetNews()
                {
                    CacheDatabase.LocalMachine.GetAndFetchLatest("news_feed",
                        () => FetchNewsFromApi())
                        .Subscribe(ProcessNewsUpdate);
                        
                    return _updateEvents
                        .Select(_ => _currentNews.ToList())
                        .StartWith(_currentNews.ToList());
                }

                public IObservable<NewsUpdateEvent> UpdateEvents => _updateEvents.AsObservable();

                private void ProcessNewsUpdate(List<NewsItem> freshNews)
                {
                    if (!_hasCachedData)
                    {
                        // First emission: cached data or initial fresh data
                        _currentNews.Clear();
                        _currentNews.AddRange(freshNews);
                        _hasCachedData = true;
                        
                        _updateEvents.OnNext(new NewsUpdateEvent
                        {
                            Type = UpdateType.InitialLoad,
                            ItemsCount = freshNews.Count
                        });
                    }
                    else
                    {
                        // Second emission: fresh data - perform differential update
                        var changes = CalculateChanges(freshNews);
                        ApplyChanges(changes);
                        
                        _updateEvents.OnNext(new NewsUpdateEvent
                        {
                            Type = UpdateType.Refresh,
                            NewItems = changes.NewItems,
                            UpdatedItems = changes.UpdatedItems,
                            RemovedItems = changes.RemovedItems
                        });
                    }
                }

                private NewsChanges CalculateChanges(List<NewsItem> freshNews)
                {
                    var changes = new NewsChanges();
                    
                    // Find new and updated items
                    foreach (var freshItem in freshNews)
                    {
                        var existingItem = _currentNews.FirstOrDefault(c => c.Id == freshItem.Id);
                        if (existingItem == null)
                        {
                            changes.NewItems.Add(freshItem);
                        }
                        else if (existingItem.LastModified < freshItem.LastModified)
                        {
                            changes.UpdatedItems.Add(freshItem);
                        }
                    }
                    
                    // Find removed items
                    var freshIds = freshNews.Select(n => n.Id).ToHashSet();
                    changes.RemovedItems.AddRange(
                        _currentNews.Where(existing => !freshIds.Contains(existing.Id)));
                    
                    return changes;
                }

                private void ApplyChanges(NewsChanges changes)
                {
                    // Remove deleted items
                    foreach (var removedItem in changes.RemovedItems)
                    {
                        _currentNews.Remove(removedItem);
                    }
                    
                    // Update existing items
                    foreach (var updatedItem in changes.UpdatedItems)
                    {
                        var index = _currentNews.FindIndex(n => n.Id == updatedItem.Id);
                        if (index >= 0)
                        {
                            _currentNews[index] = updatedItem;
                        }
                    }
                    
                    // Add new items
                    _currentNews.AddRange(changes.NewItems);
                    
                    // Sort by publication date
                    _currentNews.Sort((a, b) => b.PublishedAt.CompareTo(a.PublishedAt));
                }
            }

            private static IObservable<List<NewsItem>> FetchNewsFromApi()
            {
                return Observable.FromAsync(async () =>
                {
                    await Task.Delay(1200); // Simulate API call
                    return new List<NewsItem>
                    {
                        new() { Id = 1, Title = "Breaking News", PublishedAt = DateTimeOffset.Now.AddHours(-2), LastModified = DateTimeOffset.Now },
                        new() { Id = 2, Title = "Tech Update", PublishedAt = DateTimeOffset.Now.AddHours(-1), LastModified = DateTimeOffset.Now },
                        new() { Id = 3, Title = "Latest Story", PublishedAt = DateTimeOffset.Now, LastModified = DateTimeOffset.Now }
                    };
                });
            }
        }

        /// <summary>
        /// Pattern 4: UI Loading States - Provide responsive feedback to users.
        /// Best for applications where user experience is critical.
        /// </summary>
        public static class LoadingStatesPattern
        {
            public class ProductService
            {
                public IObservable<DataState<List<Product>>> GetProducts()
                {
                    // Start with loading state
                    var loadingState = Observable.Return(DataState<List<Product>>.Loading());
                    
                    // Get cached and fresh data
                    var dataStream = CacheDatabase.LocalMachine.GetAndFetchLatest("products",
                        () => FetchProductsFromApi())
                        .Select(products => DataState<List<Product>>.Success(products, products.Count))
                        .Catch<DataState<List<Product>>, Exception>(ex => 
                            Observable.Return(DataState<List<Product>>.Error(ex)));
                    
                    // Combine loading state with data stream
                    return loadingState.Concat(dataStream);
                }
            }

            // Usage example with ViewModel
            public class ProductViewModel
            {
                private readonly ProductService _productService = new();
                
                public bool IsLoading { get; private set; }
                public List<Product> Products { get; private set; } = new();
                public string? ErrorMessage { get; private set; }

                public void LoadProducts()
                {
                    _productService.GetProducts()
                        .Subscribe(state => HandleDataState(state));
                }

                private void HandleDataState(DataState<List<Product>> state)
                {
                    switch (state.Status)
                    {
                        case DataStatus.Loading:
                            IsLoading = true;
                            ErrorMessage = null;
                            Console.WriteLine("Loading products...");
                            break;
                            
                        case DataStatus.Success:
                            IsLoading = false;
                            Products = state.Data ?? new List<Product>();
                            ErrorMessage = null;
                            Console.WriteLine($"Loaded {Products.Count} products (from {state.Source})");
                            break;
                            
                        case DataStatus.Error:
                            IsLoading = false;
                            ErrorMessage = state.Error?.Message ?? "Unknown error";
                            Console.WriteLine($"Error loading products: {ErrorMessage}");
                            break;
                    }
                }
            }

            private static IObservable<List<Product>> FetchProductsFromApi()
            {
                return Observable.FromAsync(async () =>
                {
                    await Task.Delay(2000); // Simulate slow API
                    return new List<Product>
                    {
                        new() { Id = 1, Name = "Laptop", Price = 999.99m },
                        new() { Id = 2, Name = "Mouse", Price = 29.99m },
                        new() { Id = 3, Name = "Keyboard", Price = 79.99m }
                    };
                });
            }
        }

        /// <summary>
        /// Pattern 5: Conditional Fetching - Control when fresh data should be fetched.
        /// Best for optimizing network usage and respecting user preferences.
        /// </summary>
        public static class ConditionalFetchingPattern
        {
            public static void TimeBasedFetching()
            {
                // Only fetch fresh data if cached data is older than 5 minutes
                CacheDatabase.LocalMachine.GetAndFetchLatest("weather_data",
                    () => FetchWeatherFromApi(),
                    fetchPredicate: cachedDate => DateTimeOffset.Now - cachedDate > TimeSpan.FromMinutes(5))
                    .Subscribe(weather => 
                    {
                        Console.WriteLine($"Weather update: {weather.Temperature}°C (Humidity: {weather.Humidity}%)");
                    });
            }

            public static void UserPreferenceBasedFetching(UserPreferences userPrefs)
            {
                // Respect user's background refresh preference
                CacheDatabase.LocalMachine.GetAndFetchLatest("user_settings",
                    () => FetchUserSettingsFromApi(),
                    fetchPredicate: _ => userPrefs.AllowBackgroundRefresh)
                    .Subscribe(settings => 
                    {
                        Console.WriteLine($"Settings updated: Theme={settings.Theme}, Notifications={settings.NotificationsEnabled}");
                    });
            }

            public static void NetworkAwareFetching()
            {
                // Only fetch on WiFi to save mobile data
                CacheDatabase.LocalMachine.GetAndFetchLatest("large_dataset",
                    () => FetchLargeDatasetFromApi(),
                    fetchPredicate: _ => IsOnWiFi())
                    .Subscribe(data => 
                    {
                        Console.WriteLine($"Large dataset updated: {data.Items.Count} items");
                    });
            }

            private static IObservable<WeatherData> FetchWeatherFromApi()
            {
                return Observable.FromAsync(async () =>
                {
                    await Task.Delay(500);
                    return new WeatherData { Temperature = 22, Humidity = 65 };
                });
            }

            private static IObservable<UserSettings> FetchUserSettingsFromApi()
            {
                return Observable.FromAsync(async () =>
                {
                    await Task.Delay(300);
                    return new UserSettings { Theme = "Dark", NotificationsEnabled = true };
                });
            }

            private static IObservable<LargeDataset> FetchLargeDatasetFromApi()
            {
                return Observable.FromAsync(async () =>
                {
                    await Task.Delay(3000); // Simulate large download
                    return new LargeDataset { Items = Enumerable.Range(1, 1000).ToList() };
                });
            }

            private static bool IsOnWiFi()
            {
                // In real app, check network type
                return true;
            }
        }

        /// <summary>
        /// Common anti-patterns and how to avoid them.
        /// </summary>
        public static class AntiPatterns
        {
            /// <summary>
            /// ❌ DON'T: Await GetAndFetchLatest - you'll only get the first result.
            /// </summary>
            public static async Task IncorrectAwaiting()
            {
                // This only gets the cached data (if available) and misses the fresh data!
                var data = await CacheDatabase.LocalMachine
                    .GetAndFetchLatest("key", () => FetchDataFromApi())
                    .FirstAsync();
                
                Console.WriteLine("Only got first result - missing fresh data!");
            }

            /// <summary>
            /// ❌ DON'T: Mix separate cached retrieval with GetAndFetchLatest.
            /// </summary>
            public static void IncorrectSeparateRetrieval()
            {
                // This creates unnecessary complexity and potential race conditions
                var cached = CacheDatabase.LocalMachine.GetObject<string>("key")
                    .FirstOrDefaultAsync();
                
                CacheDatabase.LocalMachine.GetAndFetchLatest("key", () => FetchDataFromApi())
                    .Subscribe(fresh => { /* handle fresh data */ });
                
                // Now you have to manually coordinate cached and fresh data!
            }

            /// <summary>
            /// ❌ DON'T: Clear collections in subscriber - will clear twice!
            /// </summary>
            public static void IncorrectCollectionClearing()
            {
                var items = new List<string>();
                
                CacheDatabase.LocalMachine.GetAndFetchLatest("items", () => FetchDataFromApi())
                    .Subscribe(data => 
                    {
                        items.Clear(); // This will clear both cached and fresh data!
                        items.AddRange(data);
                    });
            }

            /// <summary>
            /// ✅ CORRECT: Use the simple replacement pattern instead.
            /// </summary>
            public static void CorrectApproach()
            {
                var items = new List<string>();
                
                CacheDatabase.LocalMachine.GetAndFetchLatest("items", () => FetchDataFromApi())
                    .Subscribe(data => 
                    {
                        // Simply replace the entire collection - works for both calls
                        items = data.ToList();
                        UpdateUI(items);
                    });
            }

            private static IObservable<List<string>> FetchDataFromApi()
            {
                return Observable.Return(new List<string> { "item1", "item2", "item3" });
            }

            private static void UpdateUI(List<string> items)
            {
                Console.WriteLine($"UI updated with {items.Count} items");
            }
        }
    }

    #region Supporting Types

    public class UserProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTimeOffset LastUpdated { get; set; }
    }

    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    public class NewsItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset PublishedAt { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class WeatherData
    {
        public int Temperature { get; set; }
        public int Humidity { get; set; }
    }

    public class UserSettings
    {
        public string Theme { get; set; } = string.Empty;
        public bool NotificationsEnabled { get; set; }
    }

    public class LargeDataset
    {
        public List<int> Items { get; set; } = new();
    }

    public class UserPreferences
    {
        public bool AllowBackgroundRefresh { get; set; }
    }

    public enum DataStatus
    {
        Loading,
        Success,
        Error
    }

    public class DataState<T>
    {
        public DataStatus Status { get; init; }
        public T? Data { get; init; }
        public Exception? Error { get; init; }
        public string Source { get; init; } = "unknown";
        public int? Count { get; init; }

        public static DataState<T> Loading() => new() { Status = DataStatus.Loading };
        
        public static DataState<T> Success(T data, int? count = null, string source = "cache/api") => 
            new() { Status = DataStatus.Success, Data = data, Count = count, Source = source };
        
        public static DataState<T> Error(Exception error) => 
            new() { Status = DataStatus.Error, Error = error };
    }

    public enum UpdateType
    {
        InitialLoad,
        Refresh
    }

    public class NewsUpdateEvent
    {
        public UpdateType Type { get; set; }
        public int ItemsCount { get; set; }
        public List<NewsItem> NewItems { get; set; } = new();
        public List<NewsItem> UpdatedItems { get; set; } = new();
        public List<NewsItem> RemovedItems { get; set; } = new();
    }

    public class NewsChanges
    {
        public List<NewsItem> NewItems { get; set; } = new();
        public List<NewsItem> UpdatedItems { get; set; } = new();
        public List<NewsItem> RemovedItems { get; set; } = new();
    }

    #endregion
}