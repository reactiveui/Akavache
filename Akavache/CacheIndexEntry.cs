using System;

namespace Akavache
{
    public class CacheIndexEntry
    {
        public DateTimeOffset CreatedAt { get; protected set; }
        public DateTimeOffset? ExpiresAt { get; protected set; }

        public CacheIndexEntry(DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }
    }
}