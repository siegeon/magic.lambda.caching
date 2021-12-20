﻿/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using magic.node;
using magic.node.contracts;
using magic.node.extensions;
using magic.signals.contracts;
using magic.lambda.caching.contracts;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.try-get] slot for checking if an item exists in the cache, and if so,
    /// returning it as is - If item doesn't exists, the slot will invoke your [.lambda]
    /// callback, to create item, store item into cache, and return the item to the caller.
    /// </summary>
    [Slot(Name = "cache.try-get")]
    public class CacheTryGet : ISlotAsync, ISlot
    {
        readonly IMagicCache _cache;
        readonly IRootResolver _rootResolver;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        /// <param name="rootResolver">Needed to be able to filter away internally hidden cache items.</param>
        public CacheTryGet(IMagicCache cache, IRootResolver rootResolver)
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
            var args = GetArgs(input);

            input.Value = _cache.GetOrCreate(args.Key, () =>
            {
                var result = new Node();
                signaler.Scope("slots.result", result, () =>
                {
                    signaler.Signal("eval", args.Lambda.Clone());
                });
                return (result.Value ?? result.Clone(), args.UtcExpires);
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

            input.Value = await _cache.GetOrCreateAsync(args.Key, async () =>
            {
                var result = new Node();
                await signaler.ScopeAsync("slots.result", result, async () =>
                {
                    await signaler.SignalAsync("eval", args.Lambda.Clone());
                });
                return (result.Value ?? result.Clone(), args.UtcExpires);
            });
            input.Clear();
        }

        #region [ -- Private helper methods -- ]

        /*
         * Returns arguments specified to invocation.
         */
        (string Key, Node Lambda, DateTime UtcExpires) GetArgs(Node input)
        {
            var key = _rootResolver.RootFolder +
                (input.GetEx<string>() ?? throw new HyperlambdaException("[cache.try-get] must be given a key"));

            var expiration = input
                .Children
                .FirstOrDefault(x => x.Name == "expiration")?.GetEx<long>() ?? 5;

            var lambda = input
                .Children
                .FirstOrDefault(x => x.Name == ".lambda") ?? 
                throw new HyperlambdaException("[cache.try-get] must have a [.lambda]");

            return (key, lambda, DateTime.UtcNow.AddSeconds(expiration));
        }

        #endregion
    }
}
