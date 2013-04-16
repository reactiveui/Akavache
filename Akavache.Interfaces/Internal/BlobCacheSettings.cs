using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Akavache
{
    // This is here due to static usages of these in BlobCache
    static class BlobCacheSettings
    {
        public static JsonSerializerSettings SerializerSettings { get; set; }
    }
}
