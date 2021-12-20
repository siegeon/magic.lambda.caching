/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System.Linq;
using magic.node;
using magic.node.contracts;
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
        readonly IRootResolver _rootResolver;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        /// <param name="rootResolver">Needed to be able to filter away internally hidden cache items.</param>
        public CacheList(IMagicMemoryCache cache, IRootResolver rootResolver)
        {
            _cache = cache;
            _rootResolver = rootResolver;
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            var offset = input
                .Children
                .FirstOrDefault(x => x.Name == "offset")?
                .GetEx<int>() ?? 0;

            var limit = input
                .Children
                .FirstOrDefault(x => x.Name == "limit")?
                .GetEx<int>() ?? 10;

            var filter = _rootResolver.RootFolder +
                input
                    .Children
                    .FirstOrDefault(x => x.Name == "filter")?
                    .GetEx<string>();

            input.Clear();
            var items = _cache
                .Items(filter)
                .Skip(offset)
                .Take(limit)
                .OrderBy(x => x.Key);

            input.AddRange(
                items
                    .Select(x => new Node(".", null, new Node[] {
                        new Node("key", x.Key.Substring(_rootResolver.RootFolder.Length)),
                        new Node("value", x.Value is Node nodeValue ? nodeValue.ToHyperlambda() : x.Value)
                    })));
        }
    }
}
