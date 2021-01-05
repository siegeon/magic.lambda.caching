/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace magic.lambda.caching.helpers
{
    /// <summary>
    /// Memory cache extension helper methods to avoid breaking existing interface of IMemoryCache too much.
    /// </summary>
    public static class MagicMemoryCacheExtensions
    {
        public static T Set<T>(this IMagicMemoryCache cache, object key, T value)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.Value = value;
                return value;
            }
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, CacheItemPriority priority)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.Priority = priority;
                entry.Value = value;
                return value;
            }
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, DateTimeOffset absoluteExpiration)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.AbsoluteExpiration = absoluteExpiration;
                entry.Value = value;
                return value;
            }
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
                entry.Value = value;
                return value;
            }
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, MemoryCacheEntryOptions options)
        {
            using (var entry = cache.CreateEntry(key))
            {
                if (options != null)
                    entry.SetOptions(options);
                entry.Value = value;
                return value;
            }
        }

        public static object Get(this IMagicMemoryCache cache, object key)
        {
            if (cache.TryGetValue(key, out object result))
                return result;
            return null;
        }

        public static TItem GetOrCreate<TItem>(this IMagicMemoryCache cache, object key, Func<ICacheEntry, TItem> factory)
        {
            if (!cache.TryGetValue(key, out var result))
            {
                using (var entry = cache.CreateEntry(key))
                {
                    result = factory(entry);
                    entry.SetValue(result);
                }
            }

            return (TItem)result;
        }

        public static async Task<TItem> GetOrCreateAsync<TItem>(this IMagicMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
        {
            if (!cache.TryGetValue(key, out object result))
            {
                using (var entry = cache.CreateEntry(key))
                {
                    result = await factory(entry);
                    entry.SetValue(result);
                }
            }
            return (TItem)result;
        }
    }
}
