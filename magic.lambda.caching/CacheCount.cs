/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;
using magic.lambda.caching.helpers;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.count] slot returning count of all cache items matching
    /// optional filter to caller.
    /// </summary>
    [Slot(Name = "cache.count")]
    public class CacheCount : ISlot
    {
        readonly IMagicMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheCount(IMagicMemoryCache cache)
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
            var filter = input.Children.FirstOrDefault(x => x.Name == "filter")?.GetEx<string>();
            input.Clear();
            var count = string.IsNullOrEmpty(filter) ?
                _cache
                    .Items()
                    .Count() :
                _cache
                    .Items()
                    .Count(x => x.Key.StartsWith(filter));
            input.Value = count;
        }
    }
}
