/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System.Linq;
using magic.node;
using magic.node.contracts;
using magic.node.extensions;
using magic.signals.contracts;
using magic.lambda.caching.contracts;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.count] slot returning the number of cacheds items matching
    /// optional filter to caller.
    /// </summary>
    [Slot(Name = "cache.count")]
    public class CacheCount : ISlot
    {
        readonly IMagicCache _cache;
        readonly IRootResolver _rootResolver;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        /// <param name="rootResolver">Needed to be able to namespace cache items.</param>
        public CacheCount(IMagicCache cache, IRootResolver rootResolver)
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
            var count = _cache.Items(filter);
            input.Value = count.Count();
        }
    }
}
