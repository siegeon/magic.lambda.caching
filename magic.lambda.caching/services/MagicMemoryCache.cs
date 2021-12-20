/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using magic.lambda.caching.helpers;
using magic.lambda.caching.contracts;

namespace magic.lambda.caching.services
{
    /// <summary>
    /// Memory cache implementation class allowing developer to query keys,
    /// and clear (all) items in one go.
    /// </summary>
    public class MagicMemoryCache : IMagicCache
    {
        // Actual items in cache.
        readonly Dictionary<string, (object Value, DateTime Expires)> _items = new Dictionary<string, (object, DateTime)>();

        // Common lock to access shared resource(s).
        readonly static object _lock = new object();

        /// <inheritdoc/>
        public void Upsert(string key, object value, DateTime utcExpiration)
        {
            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Upserting item into cache.
                _items[key] = (Value: value, Expires: utcExpiration);
            }
        }

        /// <inheritdoc/>
        public void Remove(string key)
        {
            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Notice, we don't purge expired items on remove, only in get/modify/etc ...
                _items.Remove(key);
            }
        }

        /// <inheritdoc/>
        public object Get(string key)
        {
            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Retrieving item if existing from dictionary.
                return _items.TryGetValue(key, out var value) ? value.Value : null;
            }
        }

        /// <inheritdoc/>
        public void Clear(string filter = null)
        {
            // Synchronizing access to shared resource.
            lock (_lock)
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
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, object>> Items(string filter = null)
        {
            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Returning only items matching filter condition.
                if (!string.IsNullOrEmpty(filter))
                    return _items
                        .Where(x => x.Key.StartsWith(filter))
                        .Select(x => new KeyValuePair<string, object>(x.Key, x.Value.Value))
                        .ToList();

                // Returning all items, making sure we don't return expiration, but only actual content.
                return _items
                    .Select(x => new KeyValuePair<string, object>(x.Key, x.Value.Value))
                    .ToList();
            }
        }

        /// <inheritdoc/>
        public object GetOrCreate(string key, Func<(object, DateTime)> factory)
        {
            /*
             * Notice, to avoid locking entire cache as we invoke factory lambda, we
             * use MagicLocker here, which will only lock on the specified key.
             */
            using (var locker = new MagicLocker(key))
            {
                // Synchronizing access to shared resource. ORDER COUNTS!
                locker.Lock();
                lock (_lock)
                {
                    // Purging all expired items.
                    PurgeExpiredItems();

                    // Checking cache.
                    if (_items.TryGetValue(key, out var value))
                        return value.Value; // Item found in cache, and it's not expired
                }

                /*
                 * Invoking factory method.
                 *
                 * Notice, here we don't lock the global locker, but only keep the
                 * MagicLocker, which creates a semaphore on a "per key" basis.
                 *
                 * This is done to avoid locking the whole cache as we invoke
                 * the factory method, which might be expensive to execute.
                 */
                var newValue = factory();

                // Synchronizing access to shared resource.
                lock (_lock)
                {
                    _items[key] = newValue;
                    return newValue.Item1;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<object> GetOrCreateAsync(string key, Func<Task<(object, DateTime)>> factory)
        {
            /*
             * Notice, to avoid locking entire cache as we invoke factory lambda, we
             * use MagicLocker here, which will only lock on the specified key.
             */
            using (var locker = new MagicLocker(key))
            {
                // Synchronizing access to shared resource. ORDER COUNTS!
                await locker.LockAsync();
                lock (_lock)
                {
                    // Purging all expired items.
                    PurgeExpiredItems();

                    // Checking cache.
                    if (_items.TryGetValue(key, out var value))
                        return value.Value; // Item found in cache, and it's not expired
                }

                /*
                 * Invoking factory method.
                 *
                 * Notice, here we don't lock the global locker, but only keep the
                 * MagicLocker, which creates a semaphore on a "per key" basis.
                 *
                 * This is done to avoid locking the whole cache as we invoke
                 * the factory method, which might be expensive to execute.
                 */
                var newValue = await factory();

                // Synchronizing access to shared resource.
                lock (_lock)
                {
                    _items[key] = newValue;
                    return newValue.Item1;
                }
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
