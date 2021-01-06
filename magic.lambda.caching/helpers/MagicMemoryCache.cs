/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
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
                if (_items.TryGetValue(key, out var value))
                {
                    if (value.Expires > DateTime.UtcNow)
                        return value.Value;
                    else
                        _items.Remove(key);
                }
            }
            finally
            {
                _locker.Release();
            }
            return null;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _locker.Wait();
            try
            {
                _items.Clear();
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
                // Checking cache.
                if (_items.TryGetValue(key, out var value) && value.Expires > DateTime.UtcNow)
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
                // Checking cache.
                if (_items.TryGetValue(key, out var value) && value.Expires > DateTime.UtcNow)
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
    }
}
