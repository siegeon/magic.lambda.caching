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
            var offset = input.Children.FirstOrDefault(x => x.Name == "offset")?.GetEx<int>() ?? 0;
            var limit = input.Children.FirstOrDefault(x => x.Name == "limit")?.GetEx<int>() ?? 10;
            var filter = input.Children.FirstOrDefault(x => x.Name == "filter")?.GetEx<string>();
            input.Clear();
            var items = string.IsNullOrEmpty(filter) ?
                _cache
                    .Items()
                    .Skip(offset)
                    .Take(limit)
                    .OrderBy(x => x.Key) :
                _cache
                    .Items()
                    .Where(x => x.Key.StartsWith(filter))
                    .Skip(offset)
                    .Take(limit)
                    .OrderBy(x => x.Key);
            input.AddRange(
                items
                    .Select(x => new Node(".", null, new Node[] {
                        new Node("key", x.Key),
                        new Node("value", x.Value is Node nodeValue ? nodeValue.ToHyperlambda() : x.Value)
                    })));
        }
    }
}
