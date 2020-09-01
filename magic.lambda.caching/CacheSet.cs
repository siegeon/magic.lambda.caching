/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.set] slot saving its first child node's value to the memory cache.
    /// </summary>
    [Slot(Name = "cache.set")]
    public class CacheSet : ISlot
    {
        readonly IMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheSet(IMemoryCache cache)
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
            if (input.Children.Count() > 1)
                throw new ApplicationException("[cache.set] can have maximum one child node");
            signaler.Signal("eval", input);
            var key = input.GetEx<string>();
            var val = input.Children.FirstOrDefault()?.Value;
            if (val == null)
                _cache.Remove(key);
            else
                _cache.Set(key, val);
        }
    }
}
