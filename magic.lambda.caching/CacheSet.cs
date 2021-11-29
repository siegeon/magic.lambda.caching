/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
using System.Linq;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;
using magic.lambda.caching.helpers;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.set] slot saving its first child node's value to the memory cache.
    /// </summary>
    [Slot(Name = "cache.set")]
    public class CacheSet : ISlot
    {
        readonly IMagicMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheSet(IMagicMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            var key = input.GetEx<string>() ?? throw new HyperlambdaException("[cache.set] must be given a key");
            var val = input.Children.FirstOrDefault(x => x.Name == "value")?.GetEx<object>();

            // Checking if value is null, at which point we simply remove cached item.
            if (val == null)
            {
                _cache.Remove(key);
                return;
            }

            // Caller tries to actually save an object to cache.
            var expiration = input.Children.FirstOrDefault(x => x.Name == "expiration")?.GetEx<long>() ?? 5;
            _cache.Upsert(key, val, DateTime.UtcNow.AddSeconds(expiration));
        }
    }
}
