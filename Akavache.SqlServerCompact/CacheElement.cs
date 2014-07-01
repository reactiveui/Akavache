using System;

namespace Akavache.SqlServerCompact
{
    internal class CacheElement
    {
        public string Key { get; set; }
        public string TypeName { get; set; }
        public byte[] Value { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}