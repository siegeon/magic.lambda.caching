/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using magic.node;
using magic.signals.contracts;
using magic.lambda.caching.helpers;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.clear] slot clearing memory cache entirely.
    /// </summary>
    [Slot(Name = "cache.clear")]
    public class CacheClear : ISlot
    {
        readonly IMagicMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheClear(IMagicMemoryCache cache)
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
            _cache.Clear();
        }
    }
}
