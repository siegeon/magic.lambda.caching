/*
 * Aista Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
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
    /// [cache.count] slot returning the number of cacheds items matching
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
