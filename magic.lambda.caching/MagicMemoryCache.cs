/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace magic.lambda.caching
{
    /// <summary>
    /// Memory cache extension implementation class allowing developer to query keys,
    /// and clear (all) items in one go.
    /// </summary>
    public class MagicMemoryCache : IMagicMemoryCache
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<object, ICacheEntry> _items = new ConcurrentDictionary<object, ICacheEntry>();

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Underlaying memory cache to use as implementation.</param>
        public MagicMemoryCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            _cache.Dispose();
        }

        /// <inheritdoc cref="IMemoryCache.TryGetValue"/>
        public bool TryGetValue(object key, out object value)
        {
            return _cache.TryGetValue(key, out value);
        }

        /// <inheritdoc cref="IMemoryCache.CreateEntry"/>
        public ICacheEntry CreateEntry(object key)
        {
            var entry = _cache.CreateEntry(key);
            entry.RegisterPostEvictionCallback(PostEvictionCallback);
            _items.AddOrUpdate(key, entry, (o, cacheEntry) =>
            {
                cacheEntry.Value = entry;
                return cacheEntry;
            });
            return entry;
        }

        /// <inheritdoc cref="IMemoryCache.Remove"/>
        public void Remove(object key)
        {
            _cache.Remove(key);
        }

        /// <inheritdoc cref="IMagicMemoryCache.Clear"/>
        public void Clear()
        {
            foreach (var cacheEntry in _items.Keys.ToList())
            {
                _cache.Remove(cacheEntry);
            }
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return _items.Select(pair => new KeyValuePair<object, object>(pair.Key, pair.Value.Value)).GetEnumerator();
        }

        /// <summary>
        /// Returns all keys in cache.
        /// </summary>
        public IEnumerator<object> Keys => _items.Keys.GetEnumerator();

        #region [ -- Private helper methods -- ]

        /*
         * Untyped explicit implementation of base interface.
         */
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /*
         * Invoked when item is evicted from cache.
         * Needed to clean up dictionary.
         */
        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (reason != EvictionReason.Replaced)
                _items.TryRemove(key, out var _);
        }

        #endregion
    }
}
