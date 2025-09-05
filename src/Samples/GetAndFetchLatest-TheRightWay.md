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

If you specifically need to merge new messages with existing ones (like a chat app where you don't want to lose messages the user is reading), use the **Merge Strategy Pattern**:

```csharp
private readonly List<LatestMessageDto> _currentMessages = new();
private bool _isFirstLoad = true;

private void LoadMessages()
{
    _ticketService.GetMessages(_selectedTicketId)
        .Subscribe(response => 
        {
            if (_isFirstLoad)
            {
                // First call: replace all content
                _currentMessages.Clear();
                _currentMessages.AddRange(response.data);
                _isFirstLoad = false;
            }
            else
            {
                // Second call: merge new items only
                var newMessages = response.data
                    .Where(msg => !_currentMessages.Any(existing => existing.Id == msg.Id))
                    .ToList();
                    
                _currentMessages.AddRange(newMessages);
                _currentMessages = _currentMessages.OrderBy(m => m.Timestamp).ToList();
            }
            
            // Update UI with current state
            UpdateMessagesDisplay(_currentMessages);
        });
}
```

## Key Takeaways

1. **Always use Subscribe(), never await** with GetAndFetchLatest
2. **Design your UI update method to handle being called twice**
3. **Simple replacement works for 90% of scenarios**
4. **Only use complex merging when you specifically need it**
5. **Trust the pattern** - don't try to manually manage cached vs fresh data

## Your Service Layer

Your service layer implementation is actually correct:

```csharp
public IObservable<GetMessagesResponse> GetMessages(int ticketId)
{
    var cache = BlobCache.LocalMachine;
    return cache.GetAndFetchLatest(ticketId.ToString(), 
        () => GetMessagesRemote(ticketId),
        offset => true); // Always fetch fresh data
}
```

The issue was in how you consumed it, not how you created it.

## Bottom Line

**The right way is usually the simple way.** GetAndFetchLatest is designed to make caching easy - don't overcomplicate it. Start with the Simple Replacement Pattern and only add complexity if you have a specific requirement that demands it.

For comprehensive examples and additional patterns, see:
- `GetAndFetchLatestPatterns.cs` in the Samples directory
- Updated README.md GetAndFetchLatest section
- Samples README.md for quick reference