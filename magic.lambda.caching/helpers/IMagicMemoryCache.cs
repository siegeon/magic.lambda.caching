/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace magic.lambda.caching.helpers
{
    /// <summary>
    /// Memory cache extension interface allowing developer to query keys,
    /// and clear (all) items in one go.
    /// </summary>
    public interface IMagicMemoryCache
    {
        /// <summary>
        /// Creates a new entry.
        /// </summary>
        /// <param name="key">What key to use for item.</param>
        /// <param name="value">The actual value of the item.</param>
        /// <param name="utcExpiration">UTC date and time of when item expires.</param>
        void Upsert(string key, object value, DateTime utcExpiration);

        /// <summary>
        /// Removes a single entry.
        /// </summary>
        /// <param name="key">Key of item to remove.</param>
        void Remove(string key);

        /// <summary>
        /// Returns a single item.
        /// </summary>
        /// <param name="key">Key of item to remove.</param>
        /// <returns>Content of item.</returns>
        object Get(string key);

        /// <summary>
        /// Clears cache entirely.
        /// </summary>
        /// <param name="filter">Optional filter conditiong items needs to match in order to be deleted.</param>
        void Clear(string filter = null);

        /// <summary>
        /// Returns all items in cache.
        /// </summary>
        /// <returns>Enumerable of all items currently stored in cache.</returns>
        IEnumerable<KeyValuePair<string, object>> Items();

        /// <summary>
        /// Retrieves a single item from cache, and if not existing, creates the item,
        /// adds it to the cache, and returns to caller.
        /// </summary>
        /// <param name="key">Key of item to create or retrieve.</param>
        /// <param name="factory">Factory method to invoke if item cannot be found in cache.</param>
        /// <returns>Object as created and/or found in cache.</returns>
        object GetOrCreate(string key, Func<(object, DateTime)> factory);

        /// <summary>
        /// Retrieves a single item from cache, and if not existing, creates the item,
        /// adds it to the cache, and returns to caller.
        /// </summary>
        /// <param name="key">Key of item to create or retrieve.</param>
        /// <param name="factory">Factory method to invoke if item cannot be found in cache.</param>
        /// <returns>Object as created and/or found in cache.</returns>
        Task<object> GetOrCreateAsync(string key, Func<Task<(object, DateTime)>> factory);
    }
}
