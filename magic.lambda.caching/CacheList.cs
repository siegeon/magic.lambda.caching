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
    /// [cache.list] slot returning all cache items to caller.
    /// </summary>
    [Slot(Name = "cache.list")]
    public class CacheList : ISlot
    {
        readonly IMagicMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheList(IMagicMemoryCache cache)
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
            input.AddRange(
                _cache
                    .Items()
                    .Select(x => new Node(x.Key, x.Value is Node nodeValue ? nodeValue.ToHyperlambda() : x.Value)));
        }
    }
}
