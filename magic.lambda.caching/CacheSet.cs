﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
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
    [Slot(Name = "wait.cache.set")]
    public class CacheSet : ISlot
    {
        readonly IMemoryCache _cache;
        readonly IConfiguration _configuration;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheSet(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            var key = input.GetEx<string>();
            var val = input.Children.FirstOrDefault(x => x.Name == "value")?.Value;

            // Checking if value is null,l at which point we simply remove cached item.
            if (val == null)
            {
                _cache.Remove(key);
                return;
            }

            // Caller tries to actually save an object to cache.
            var expiration = input.Children.FirstOrDefault(x => x.Name == "expiration")?.GetEx<int>() ?? 
                int.Parse(_configuration["magic:caching:expiration"] ?? "5");

            var expirationType = input.Children.FirstOrDefault(x => x.Name == "expiration-type")?.GetEx<string>() ?? 
                _configuration["magic:caching:expiration-type"] ??
                "sliding";

            var options = new MemoryCacheEntryOptions();
            if (expirationType == "sliding")
            {
                options.SlidingExpiration = new TimeSpan(0, 0, expiration);
            }
            else if (expirationType == "absolute")
            {
                options.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(expiration);
            }
            else
                throw new ArgumentException($"'{expirationType}' is not a known type of expiration");
            _cache.Set(key, val, options);
        }
    }
}
