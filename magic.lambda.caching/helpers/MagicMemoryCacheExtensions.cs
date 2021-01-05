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
            var entry = cache.CreateEntry(key);
            entry.Value = value;
            entry.Dispose(); // Commits item to cache. Notice, do *not* use 'using' pattern here, since it might commit a null value to cache.
            return value;
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, CacheItemPriority priority)
        {
            var entry = cache.CreateEntry(key);
            entry.Priority = priority;
            entry.Value = value;
            entry.Dispose(); // Commits item to cache. Notice, do *not* use 'using' pattern here, since it might commit a null value to cache.
            return value;
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, DateTimeOffset absoluteExpiration)
        {
            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpiration = absoluteExpiration;
            entry.Value = value;
            entry.Dispose(); // Commits item to cache. Notice, do *not* use 'using' pattern here, since it might commit a null value to cache.
            return value;
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            entry.Value = value;
            entry.Dispose(); // Commits item to cache. Notice, do *not* use 'using' pattern here, since it might commit a null value to cache.
            return value;
        }

        public static T Set<T>(this IMagicMemoryCache cache, object key, T value, MemoryCacheEntryOptions options)
        {
            var entry = cache.CreateEntry(key);
            if (options != null)
                entry.SetOptions(options);
            entry.Value = value;
            entry.Dispose(); // Commits item to cache. Notice, do *not* use 'using' pattern here, since it might commit a null value to cache.
            return value;
        }

        public static object Get(this IMagicMemoryCache cache, object key)
        {
            return cache.TryGetValue(key, out object result) ? result : null;
        }

        public static TItem GetOrCreate<TItem>(this IMagicMemoryCache cache, object key, Func<ICacheEntry, TItem> factory)
        {
            if (!cache.TryGetValue(key, out var result))
            {
                var entry = cache.CreateEntry(key);
                result = factory(entry);
                entry.SetValue(result);
                entry.Dispose(); // Commits item to cache. Notice, do *not* use 'using' pattern here, since it might commit a null value to cache.
            }

            return (TItem)result;
        }

        public static async Task<TItem> GetOrCreateAsync<TItem>(this IMagicMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
        {
            if (!cache.TryGetValue(key, out object result))
            {
                var entry = cache.CreateEntry(key);
                result = await factory(entry);
                entry.SetValue(result);
                entry.Dispose(); // Commits item to cache. Notice, do *not* use 'using' pattern here, since it might commit a null value to cache.
            }
            return (TItem)result;
        }
    }
}
