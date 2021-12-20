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
    /// [cache.clear] slot clearing memory cache entirely, or optionally taking a filter
    /// declaring which items to clear.
    /// </summary>
    [Slot(Name = "cache.clear")]
    public class CacheClear : ISlot
    {
        readonly IMagicMemoryCache _cache;
        readonly IRootResolver _rootResolver;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        /// <param name="rootResolver">Needed to be able to namespace cache items.</param>
        public CacheClear(IMagicMemoryCache cache, IRootResolver rootResolver)
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
            var filter = _rootResolver.RootFolder +
                input
                    .Children
                    .FirstOrDefault(x => x.Name == "filter")?
                    .GetEx<string>();
            input.Clear();
            _cache.Clear(filter);
        }
    }
}
