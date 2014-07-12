using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;

namespace Akavache.SqlServerCompact
{
    internal class CacheElement
    {
        public string Key { get; set; }
        public string TypeName { get; set; }
        public byte[] Value { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime CreatedAt { get; set; }

        internal static CacheElement FromDataReader(DbDataReader result)
        {
            var cacheElement = new CacheElement
            {
                Key = result.GetFieldValue<string>(0),
                Value = result.GetFieldValue<byte[]>(2),
                CreatedAt = result.GetFieldValue<DateTime>(3),
                Expiration = result.GetFieldValue<DateTime>(4)
            };

            var typeName = result.GetValue(1);
            if (typeName != DBNull.Value)
            {
                cacheElement.TypeName = typeName.ToString();
            }
            return cacheElement;
        }

        internal static List<CacheElement> FromDataReader(SqlCeCommand command)
        {
            var list = new List<CacheElement>();

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(FromDataReader(reader));
                }

                reader.Close();
            }
            return list;
        }
    }
}