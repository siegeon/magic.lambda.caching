/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace magic.lambda.caching.helpers
{
    /// <summary>
    /// Memory cache implementation class allowing developer to query keys,
    /// and clear (all) items in one go.
    /// </summary>
    public class MagicMemoryCache : IMagicMemoryCache
    {
        // Used to synchronize access to dictionary.
        readonly SemaphoreSlim _locker = new SemaphoreSlim(1);

        // Actual items in cache.
        readonly Dictionary<string, (object Value, DateTime Expires)> _items = new Dictionary<string, (object, DateTime)>();

        /// <inheritdoc/>
        public void Upsert(string key, object value, DateTime utcExpiration)
        {
            _locker.Wait();
            try
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Upserting item into cache.
                _items[key] = (Value: value, Expires: utcExpiration);
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <inheritdoc/>
        public void Remove(string key)
        {
            _locker.Wait();
            try
            {
                // Notice, we don't purge expired items on remove, only in get/modify/etc ...
                _items.Remove(key);
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <inheritdoc/>
        public object Get(string key)
        {
            _locker.Wait();
            try
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Retrieving item if existing from dictionary.
                return _items.TryGetValue(key, out var value) ? value.Value : null;
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <inheritdoc/>
        public void Clear(string filter = null)
        {
            _locker.Wait();
            try
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    foreach (var idx in _items.Where(x => x.Key.StartsWith(filter)).ToList())
                    {
                        _items.Remove(idx.Key);
                    }
                }
                else
                {
                    _items.Clear();
                }
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, object>> Items()
        {
            _locker.Wait();
            try
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Returning all items, making sure we don't return expiration, but only actual content.
                return _items
                    .Select(x => new KeyValuePair<string, object>(x.Key, x.Value.Value))
                    .ToList();
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <inheritdoc/>
        public object GetOrCreate(string key, Func<(object, DateTime)> factory)
        {
            // Checking if item exists in cache.
            _locker.Wait();
            try
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Checking cache.
                if (_items.TryGetValue(key, out var value))
                    return value.Value; // Item found in cache, and it's not expired

                // Invoking factory method.
                value = factory();
                _items[key] = value;
                return value.Value;
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<object> GetOrCreateAsync(string key, Func<Task<(object, DateTime)>> factory)
        {
            // Checking if item exists in cache.
            await _locker.WaitAsync();
            try
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Checking cache.
                if (_items.TryGetValue(key, out var value))
                    return value.Value; // Item found in cache, and it's not expired

                // Invoking factory method.
                value = await factory();
                _items[key] = value;
                return value.Value;
            }
            finally
            {
                _locker.Release();
            }
        }

        #region [ -- Private helper methods -- ]

        /*
         * Deletes all expired cache items from dictionary.
         *
         * Notice, assumes caller has acquired a lock on dictionary!
         */
        void PurgeExpiredItems()
        {
            var now = DateTime.UtcNow;
            foreach (var idx in _items.ToList())
            {
                if (idx.Value.Expires <= now)
                    _items.Remove(idx.Key);
            }
        }

        #endregion
    }
}
