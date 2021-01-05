/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace magic.lambda.caching.helpers
{
    /// <summary>
    /// Memory cache extension interface allowing developer to query keys,
    /// and clear (all) items in one go.
    /// </summary>
    public interface IMagicMemoryCache : IMemoryCache, IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// Clears cache entirely.
        /// </summary>
        void Clear();
    }
}
