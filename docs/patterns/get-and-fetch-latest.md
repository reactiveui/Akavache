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