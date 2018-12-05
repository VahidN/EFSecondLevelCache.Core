using System.Collections.Generic;
using System.Threading;
using CacheManager.Core;
using EFSecondLevelCache.Core.Contracts;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Using ICacheManager as a cache service.
    /// </summary>
    public class EFCacheServiceProvider : IEFCacheServiceProvider
    {
        private static readonly EFCacheKey _nullObject = new EFCacheKey();
        private readonly ICacheManager<ISet<string>> _dependenciesCacheManager;
        private readonly ICacheManager<object> _valuesCacheManager;
        private readonly ReaderWriterLockSlim _vcmReaderWriterLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _dcReaderWriterLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Some cache providers won't accept null values.
        /// So we need a custom Null object here. It should be defined `static readonly` in your code.
        /// </summary>
        public object NullObject => _nullObject;

        /// <summary>
        /// Using ICacheManager as a cache service.
        /// </summary>
        public EFCacheServiceProvider(
            ICacheManager<object> valuesCacheManager,
            ICacheManager<ISet<string>> dependenciesCacheManager)
        {
            _valuesCacheManager = valuesCacheManager;
            _dependenciesCacheManager = dependenciesCacheManager;
        }

        /// <summary>
        /// Removes the cached entries added by this library.
        /// </summary>
        public void ClearAllCachedEntries()
        {
            _vcmReaderWriterLock.TryWriteLocked(() => _valuesCacheManager.Clear());
            _dcReaderWriterLock.TryWriteLocked(() => _dependenciesCacheManager.Clear());
        }

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="cacheKey">key to find</param>
        /// <returns>cached value</returns>
        public object GetValue(string cacheKey)
        {
            return _valuesCacheManager.Get(cacheKey);
        }

        /// <summary>
        /// Adds a new item to the cache.
        /// </summary>
        /// <param name="cacheKey">key</param>
        /// <param name="value">value</param>
        /// <param name="rootCacheKeys">cache dependencies</param>
        public void InsertValue(string cacheKey, object value,
                                ISet<string> rootCacheKeys)
        {
            if (value == null)
            {
                value = NullObject; // `HttpRuntime.Cache.Insert` won't accept null values.
            }

            foreach (var rootCacheKey in rootCacheKeys)
            {
                _dcReaderWriterLock.TryWriteLocked(() =>
                {
                    _dependenciesCacheManager.AddOrUpdate(rootCacheKey, new HashSet<string> { cacheKey },
                        updateValue: set =>
                        {
                            set.Add(cacheKey);
                            return set;
                        });
                });
            }

            _vcmReaderWriterLock.TryWriteLocked(() => _valuesCacheManager.Add(cacheKey, value));
        }

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="rootCacheKeys">cache dependencies</param>
        public void InvalidateCacheDependencies(string[] rootCacheKeys)
        {
            foreach (var rootCacheKey in rootCacheKeys)
            {
                if (string.IsNullOrWhiteSpace(rootCacheKey))
                {
                    continue;
                }

                clearDependencyValues(rootCacheKey);
                _dcReaderWriterLock.TryWriteLocked(() => _dependenciesCacheManager.Remove(rootCacheKey));
            }
        }

        private void clearDependencyValues(string rootCacheKey)
        {
            _dcReaderWriterLock.TryReadLocked(() =>
            {
                var dependencyKeys = _dependenciesCacheManager.Get(rootCacheKey);
                if (dependencyKeys == null)
                {
                    return;
                }

                foreach (var dependencyKey in dependencyKeys)
                {
                    _vcmReaderWriterLock.TryWriteLocked(() => _valuesCacheManager.Remove(dependencyKey));
                }
            });
        }
    }
}