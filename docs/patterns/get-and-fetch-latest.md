# GetAndFetchLatest: The Right Way

This document directly addresses the question: **"Caching, the right way? How to properly implement caching with Akavache's GetAndFetchLatest pattern."**

## Your Original Code Analysis

Looking at your original code, there are several issues that make it more complex than necessary:

```csharp
// ❌ Your original approach - overly complex
private async void LoadMessages()
{
    IObservable<GetMessagesResponse> messages = _ticketService.GetMessages(_selectedTicketId);
    
    List<LatestMessageDto> serverResult = new List<LatestMessageDto>();
    List<LatestMessageDto> cachedResult = new List<LatestMessageDto>();

    // This method is fired twice (Cache + Remote data)
    messages.Subscribe(subscribedPosts => 
    {
        serverResult.Clear();  // ❌ Clearing on each call
        serverResult.AddRange(subscribedPosts.data);
        
        // ❌ Complex difference calculation
        var newItems = serverResult.Except(cachedResult, new MessageComparer()).Reverse().ToList();
        AddMessages(newItems);
    });

    // ❌ Separate await for cached data
    var cache = await messages.FirstOrDefaultAsync();
    cachedResult.AddRange(cache.data);
    cachedResult.Reverse();
    AddMessages(cachedResult);
}
```

### Problems with Your Approach:
1. **Mixing await with Subscribe** - You're getting cached data separately, defeating the purpose
2. **Manual state management** - Tracking serverResult and cachedResult separately 
3. **Complex difference calculation** - Manually using Except() and custom comparers
4. **Double UI updates** - Calling AddMessages() in both Subscribe and separately
5. **Clearing collections** - This happens twice, once for cached and once for fresh data

## ✅ The Right Way: Simple Replacement Pattern

For most scenarios, especially yours, use the **Simple Replacement Pattern**:

```csharp
private void LoadMessages()
{
    _ticketService.GetMessages(_selectedTicketId)
        .Subscribe(response => 
        {
            // This is called twice: cached data first, then fresh data
            // Simply replace the UI content each time - it works perfectly!
            
            var messages = response.data.ToList();
            ReplaceAllMessages(messages); // Replace entire UI content
            
            // Optional: Show that data was updated
            UpdateTimestamp = DateTime.Now;
        });
}

private void ReplaceAllMessages(List<LatestMessageDto> messages)
{
    // Clear and replace - this method handles being called twice
    Messages.Clear();
    Messages.AddRange(messages);
    
    // Sort if needed
    Messages = Messages.OrderBy(m => m.Timestamp).ToList();
}
```

## Why This Works Better

1. **Single subscription** - GetAndFetchLatest handles both cached and fresh data
2. **No manual state tracking** - Let the pattern handle the complexity
3. **Simple UI updates** - Just replace content each time
4. **Works for both calls** - UI update logic is the same for cached and fresh data
5. **Much less code** - Simpler and more maintainable

## When You Need Merging

If you really need to merge new data with existing data (rare), use this pattern:

```csharp
private void LoadMessagesWithMerging()
{
    bool isFirstUpdate = true;
    
    _ticketService.GetMessages(_selectedTicketId)
        .Subscribe(response => 
        {
            var newMessages = response.data.ToList();
            
            if (isFirstUpdate)
            {
                // First call (cached data) - replace everything
                ReplaceAllMessages(newMessages);
                isFirstUpdate = false;
            }
            else
            {
                // Second call (fresh data) - smart merge
                MergeNewMessages(newMessages);
            }
        });
}

private void MergeNewMessages(List<LatestMessageDto> newMessages)
{
    // Add only truly new messages
    var existingIds = Messages.Select(m => m.Id).ToHashSet();
    var trulyNewMessages = newMessages.Where(m => !existingIds.Contains(m.Id));
    
    Messages.AddRange(trulyNewMessages);
    Messages = Messages.OrderBy(m => m.Timestamp).ToList();
}
```

### Pattern 3: Differential Updates with State Tracking

Best for complex scenarios where you need fine-grained control:

```csharp
public class NewsService   
{  
    private readonly Subject<List<NewsItem>> _newsSubject = new();  
    private List<NewsItem> _cachedNews = new();  
    private bool _hasCachedData = false;

    public IObservable<List<NewsItem>> GetNews()  
    {  
        CacheDatabase.LocalMachine.GetAndFetchLatest("news_feed",  
            () => newsApi.GetLatestNews())  
           .Subscribe(freshNews =>   
            {  
                if (!_hasCachedData)  
                {  
                    // First emission: cached data (or first fresh data if no cache)  
                    _cachedNews = freshNews.ToList();  
                    _hasCachedData = true;  
                    _newsSubject.OnNext(_cachedNews);  
                }  
                else  
                {  
                    // Second emission: fresh data - perform smart merge  
                    var updatedItems = new List<NewsItem>();  
                    var newItems = new List<NewsItem>();  
                      
                    foreach (var freshItem in freshNews)  
                    {  
                        var existingItem = _cachedNews.FirstOrDefault(c => c.Id == freshItem.Id);  
                        if (existingItem != null)  
                        {  
                            // Update existing item if content changed  
                            if (existingItem.LastModified < freshItem.LastModified)  
                            {  
                                updatedItems.Add(freshItem);  
                                var index = _cachedNews.IndexOf(existingItem);  
                                _cachedNews[index] = freshItem;  
                            }  
                        }  
                        else  
                        {  
                            // New item  
                            newItems.Add(freshItem);  
                            _cachedNews.Add(freshItem);  
                        }  
                    }  
                      
                    // Remove items that no longer exist  
                    _cachedNews.RemoveAll(cached => !freshNews.Any(fresh => fresh.Id == cached.Id));  
                      
                    // Notify subscribers with current state  
                    _newsSubject.OnNext(_cachedNews.ToList());  
                      
                    // Optional: Emit specific change notifications  
                    if (newItems.Any()) OnNewItemsAdded?.Invoke(newItems);  
                    if (updatedItems.Any()) OnItemsUpdated?.Invoke(updatedItems);  
                }  
            });  
              
        return _newsSubject.AsObservable();  
    }  
}
```

### **Pattern 4: UI Loading States**

Best for providing responsive UI feedback:

```csharp
public class DataService   
{  
    public IObservable<DataState<List<Product>>> GetProducts()  
    {  
        var loadingState = Observable.Return(DataState<List<Product>>.Loading());  
          
        var dataStream = CacheDatabase.LocalMachine.GetAndFetchLatest("products",  
            () => productApi.GetProducts())  
           .Select(products => DataState<List<Product>>.Success(products))  
           .Catch<DataState<List<Product>>, Exception>(ex =>   
                Observable.Return(DataState<List<Product>>.Error(ex)));  
          
        return loadingState.Concat(dataStream);  
    }  
}

// Usage in ViewModel  
public class ProductViewModel   
{  
    public ProductViewModel()  
    {  
        _dataService.GetProducts()  
           .Subscribe(state =>   
            {  
                switch (state.Status)  
                {  
                    case DataStatus.Loading:  
                        IsLoading = true;  
                        break;  
                    case DataStatus.Success:  
                        IsLoading = false;  
                        Products = state.Data;  
                        break;  
                    case DataStatus.Error:  
                        IsLoading = false;  
                        ErrorMessage = state.Error?.Message;  
                        break;  
                }  
            });  
    }  
}
```

### **Pattern 5: Conditional Fetching**

Control when fresh data should be fetched:

```csharp
// Only fetch fresh data if cached data is older than 5 minutes  
CacheDatabase.LocalMachine.GetAndFetchLatest("weather_data",  
    () => weatherApi.GetCurrentWeather(),  
    fetchPredicate: cachedDate => DateTimeOffset.Now - cachedDate > TimeSpan.FromMinutes(5))  
   .Subscribe(weather => UpdateWeatherDisplay(weather));

// Fetch fresh data based on user preference  
CacheDatabase.LocalMachine.GetAndFetchLatest("user_settings",  
    () => settingsApi.GetUserSettings(),  
    fetchPredicate: _ => userPreferences.AllowBackgroundRefresh)  
   .Subscribe(settings => ApplySettings(settings));
```

### **Common Anti-Patterns ❌**

```csharp
// ❌ DON'T: Await GetAndFetchLatest - you'll only get first result  
var data = await CacheDatabase.LocalMachine.GetAndFetchLatest("key", fetchFunc).FirstAsync();

// ❌ DON'T: Mix cached retrieval with GetAndFetchLatest  
var cached = await cache.GetObject<T>("key").FirstOrDefaultAsync();  
cache.GetAndFetchLatest("key", fetchFunc).Subscribe(fresh => /* handle fresh */);

// ❌ DON'T: Ignore the dual nature in UI updates  
cache.GetAndFetchLatest("key", fetchFunc)  
   .Subscribe(data => items.Clear()); // This will clear twice!
```

### **Best Practices ✅**

1. **Always use Subscribe(), never await** - GetAndFetchLatest is designed for reactive scenarios  
2. **Handle both cached and fresh data appropriately** - Design your subscriber to work correctly when called 1-2 times (once if no cache, twice if cached data exists)  
3. **Use state tracking for complex merges** - Keep track of whether you're handling cached or fresh data  
4. **Provide loading indicators** - Show users when fresh data is being fetched  
5. **Handle errors gracefully** - Network calls can fail, always have fallback logic  
6. **Consider using fetchPredicate** - Avoid unnecessary network calls when cached data is still fresh  
7. **Test empty cache scenarios** - Ensure your app works correctly on first run or after cache clears

## Key Takeaways

1. **Start simple** - Use the Simple Replacement Pattern first
2. **GetAndFetchLatest calls your subscriber twice** - once for cached data, once for fresh
3. **Don't overthink it** - Replacing UI content twice is usually fine and often preferred
4. **Avoid manual state tracking** - Let the pattern handle complexity
5. **Only add merging logic if you have a specific requirement** for it

## Your Service Layer

Make sure your service method is properly implemented:

```csharp
public IObservable<GetMessagesResponse> GetMessages(int ticketId)
{
    var cacheKey = $"messages_{ticketId}";
    var expiry = TimeSpan.FromMinutes(10);
    
    return CacheDatabase.UserAccount.GetAndFetchLatest(
        cacheKey,
        () => _apiClient.GetMessagesAsync(ticketId), // Your HTTP call
        expiry
    );
}
```

## Empty Cache Scenarios

**Important:** GetAndFetchLatest handles empty cache gracefully. If there's no cached data:
- Your subscriber is called once (not twice)
- You get fresh data from the fetch function
- The fresh data is automatically cached for next time

This means your UI code doesn't need to handle "empty cache" as a special case.

## Bottom Line

**The right way is usually the simple way.** GetAndFetchLatest is designed to make caching easy - don't overcomplicate it. Start with the Simple Replacement Pattern and only add complexity if you have a specific requirement that demands it.

For comprehensive examples and additional patterns, see:
- [`GetAndFetchLatestPatterns.cs`](../../src/Samples/GetAndFetchLatestPatterns.cs) in the Samples directory
- Updated README.md GetAndFetchLatest section
- Samples README.md for quick reference
