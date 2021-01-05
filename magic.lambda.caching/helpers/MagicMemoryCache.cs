/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace magic.lambda.caching.helpers
{
    /// <summary>
    /// Memory cache extension implementation class allowing developer to query keys,
    /// and clear (all) items in one go.
    /// </summary>
    public class MagicMemoryCache : IMagicMemoryCache
    {
        /*
         * Actual implementation, simply using default IMemoryCache instance.
         */
        readonly IMemoryCache _cache;

        /*
         * Duplicated dictionary. Looks a bit stupid, but is necessary to iterate cache items,
         * and clear all items altogether.
         */
        readonly ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Underlaying memory cache to use as implementation.</param>
        public MagicMemoryCache(IMemoryCache cache)
        {
            _cache = cache;
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
            _items.TryAdd(key.ToString(), new object());
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
            foreach (var cacheEntry in this)
            {
                _cache.Remove(cacheEntry.Key);
            }
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var idx in _items.Keys.ToList())
            {
                if (_cache.TryGetValue(idx, out object value))
                    yield return new KeyValuePair<string, object>(idx, value);
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            _cache.Dispose();
        }

        #region [ -- Private helper methods -- ]

        /*
         * Untyped explicit implementation of interface.
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
                _items.TryRemove(key.ToString(), out var _);
        }

        #endregion
    }
}
