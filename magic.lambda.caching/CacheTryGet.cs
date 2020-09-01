/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.try-get] slot saving its first child node's value to the memory cache.
    /// </summary>
    [Slot(Name = "cache.try-get")]
    [Slot(Name = "wait.cache.try-get")]
    public class CacheTryGet : ISlotAsync, ISlot
    {
        readonly IMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheTryGet(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            if (input.Children.Count() != 1)
                throw new ApplicationException("[cache.try-get] must have exactly one child node");
            var key = input.GetEx<string>();
            input.Value = _cache.GetOrCreate(key, (entry) =>
            {
                signaler.Signal("eval", input);
                return input.Children.FirstOrDefault()?.Value;
            });
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            if (input.Children.Count() != 1)
                throw new ApplicationException("[cache.try-get] must have exactly one child node");
            var key = input.GetEx<string>();
            input.Value = await _cache.GetOrCreate(key, async (entry) =>
            {
                await signaler.SignalAsync("wait.eval", input);
                return input.Children.FirstOrDefault()?.Value;
            });
        }
    }
}
