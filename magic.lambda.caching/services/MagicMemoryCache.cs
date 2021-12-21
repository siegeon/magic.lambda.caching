/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using magic.node.contracts;
using magic.node.extensions;
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
        readonly Dictionary<string, (object Value, DateTime Expires)> _items = new Dictionary<string, (object, DateTime)>();
        readonly static object _lock = new object();
        readonly IRootResolver _rootResolver;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="rootResolver">Needed to correctly resolve cache items.</param>
        public MagicMemoryCache(IRootResolver rootResolver)
        {
            _rootResolver = rootResolver;
        }

        /// <inheritdoc/>
        public void Upsert(string key, object value, DateTime utcExpiration, bool hidden = false)
        {
            // Sanity checking invocation.
            if (utcExpiration < DateTime.UtcNow)
                throw new HyperlambdaException($"You cannot upsert a new item into your cache with an expiration date that is in the past. Cache key of item that created conflict was '{key}'");

            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Upserting item into cache.
                _items[GetKey(key, hidden)] = (Value: value, Expires: utcExpiration);
            }
        }

        /// <inheritdoc/>
        public void Remove(string key, bool hidden = false)
        {
            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Notice, we don't purge expired items on remove, only in get/modify/etc ...
                _items.Remove(GetKey(key, hidden));
            }
        }

        /// <inheritdoc/>
        public object Get(string key, bool hidden = false)
        {
            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Purging all expired items.
                PurgeExpiredItems();

                // Retrieving item if existing from dictionary.
                return _items.TryGetValue(GetKey(key, hidden), out var value) ? value.Value : null;
            }
        }

        /// <inheritdoc/>
        public void Clear(string filter = null, bool hidden = false)
        {
            // Prepending root value to cache filter.
            filter = GetFilter(filter, hidden);

            // Synchronizing access to shared resource.
            lock (_lock)
            {
                foreach (var idx in _items.Where(x => x.Key.StartsWith(filter)).ToList())
                {
                    _items.Remove(idx.Key);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, object>> Items(string filter = null, bool hidden = false)
        {
            // Prepending root value to cache filter.
            filter = GetFilter(filter, hidden);

            // Synchronizing access to shared resource.
            lock (_lock)
            {
                // Purging all expired items.
                PurgeExpiredItems();
                return _items
                    .Where(x => x.Key.StartsWith(filter))
                    .Select(x => 
                        new KeyValuePair<string, object>(
                            x.Key.Substring(_rootResolver.RootFolder.Length + 1),
                            x.Value.Value))
                    .ToList();
            }
        }

        /// <inheritdoc/>
        public object GetOrCreate(string key, Func<(object, DateTime)> factory, bool hidden = false)
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
                    if (_items.TryGetValue(GetKey(key, hidden), out var value))
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

                // Sanity checking invocation.
                if (newValue.Item2 < DateTime.UtcNow)
                    throw new HyperlambdaException($"You cannot insert a new item into your cache with an expiration date that is in the past. Cache key of item that created conflict was '{key}'");

                // Synchronizing access to shared resource.
                lock (_lock)
                {
                    _items[GetKey(key, hidden)] = newValue;
                    return newValue.Item1;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<object> GetOrCreateAsync(string key, Func<Task<(object, DateTime)>> factory, bool hidden = false)
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
                    if (_items.TryGetValue(GetKey(key, hidden), out var value))
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
                    _items[GetKey(key, hidden)] = newValue;
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

        /*
         * Common method to create a unique key according to privacy settings.
         */
        string GetKey(string key, bool hidden)
        {
            // Sanity checking invocation.
            if (string.IsNullOrEmpty(key))
                throw new HyperlambdaException("You cannot reference an item in your cache without providing us with a key.");
            if (key.StartsWith(".") || key.StartsWith("+"))
                throw new HyperlambdaException($"You cannot reference an new item in your cache that starts with a period (.) or a plus (+) - Cache key of item that created conflict was '{key}'");

            // Returning unique key to caller.
            return _rootResolver.RootFolder + (hidden ? "." : "+") + key;
        }

        /*
         * Common method to create a unique key according to privacy settings.
         */
        string GetFilter(string filter, bool hidden)
        {
            // Sanity checking invocation.
            if (!string.IsNullOrEmpty(filter) && (filter.StartsWith(".") || filter.StartsWith("+")))
                throw new HyperlambdaException($"You cannot reference items from your cache starting with a period (.) or a (+) - Filter that created conflict was '{filter}'");

            // Returning filter to caller.
            return _rootResolver.RootFolder + (hidden ? "." : "+") + filter;
        }

        #endregion
    }
}
