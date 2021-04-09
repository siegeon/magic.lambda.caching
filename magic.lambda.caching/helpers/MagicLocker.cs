/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace magic.lambda.caching.helpers
{
    /*
     * Helper class to ensure synchronised access to cache based upon cache keys.
     *
     * This class will lock on the specified key passed into the CTOR of the class,
     * and release the semaphore/lock as the instance is disposed.
     *
     * Make sure you use the "using" pattern as you instantiate the class.
     */
    internal class MagicLocker : IDisposable
    {
        static readonly ConcurrentDictionary<string, SemaphoreSlim> _lockers = new ConcurrentDictionary<string, SemaphoreSlim>();
        readonly SemaphoreSlim _semaphore;

        public MagicLocker(string key)
        {
            _semaphore = _lockers.GetOrAdd(key, (_) => new SemaphoreSlim(1));
        }

        public void Lock()
        {
            _semaphore.Wait();
        }

        public async Task LockAsync()
        {
            await _semaphore.WaitAsync();
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
