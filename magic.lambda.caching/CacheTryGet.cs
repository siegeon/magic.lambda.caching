﻿/*
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
    public class CacheTryGet : ISlotAsync, ISlot
    {
        readonly IMagicMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheTryGet(IMagicMemoryCache cache)
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
            var args = GetArgs(input);

            input.Value = _cache.GetOrCreate(args.Key, entry =>
            {
                var result = new Node();
                signaler.Scope("slots.result", result, () =>
                {
                    signaler.Signal("eval", args.Lambda.Clone());
                });
                ConfigureCacheObject(entry, input);
                return result.Value ?? result.Clone();
            });
            input.Clear();
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            var args = GetArgs(input);

            input.Value = await _cache.GetOrCreateAsync(args.Key, async entry =>
            {
                var result = new Node();
                await signaler.ScopeAsync("slots.result", result, async () =>
                {
                    await signaler.SignalAsync("eval", args.Lambda.Clone());
                });
                ConfigureCacheObject(entry, input);
                return result.Value ?? result.Clone();
            });
            input.Clear();
        }

        #region [ -- Private helper methods -- ]

        /*
         * Returns arguments specified to invocation.
         */
        (string Key, Node Lambda) GetArgs(Node input)
        {
            var key = input.GetEx<string>() ?? 
                throw new ArgumentException("[cache.try-get] must be given a key");

            var lambda = input.Children.FirstOrDefault(x => x.Name == ".lambda") ??
                throw new ArgumentException("[cache.try-get] must have a [.lambda]");

            return (key, lambda);
        }

        void ConfigureCacheObject(ICacheEntry entry, Node input)
        {
            // Caller tries to actually save an object to cache.
            var expiration = input.Children.FirstOrDefault(x => x.Name == "expiration")?.GetEx<int>() ?? 5;

            var expirationType = input.Children.FirstOrDefault(x => x.Name == "expiration-type")?.GetEx<string>() ?? "sliding";

            if (expirationType == "sliding")
                entry.SlidingExpiration = new TimeSpan(0, 0, expiration);
            else if (expirationType == "absolute")
                entry.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(expiration);
            else
                throw new ArgumentException($"'{expirationType}' is not a known type of expiration");
        }

        #endregion
    }
}
