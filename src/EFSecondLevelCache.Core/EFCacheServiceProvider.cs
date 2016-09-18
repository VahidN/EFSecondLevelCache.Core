using System.Collections.Generic;
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
            _valuesCacheManager.Clear();
            _dependenciesCacheManager.Clear();
        }

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="cacheKey">key to find</param>
        /// <returns>cached value</returns>
        public object GetValue(string cacheKey)
        {
            var value = _valuesCacheManager.Get(cacheKey);
            return value == _nullObject ? null : value;
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
                value = _nullObject; // `HttpRuntime.Cache.Insert` won't accept null values.
            }

            foreach (var rootCacheKey in rootCacheKeys)
            {
                _dependenciesCacheManager.AddOrUpdate(rootCacheKey, new HashSet<string> { cacheKey },
                    updateValue: set =>
                                    {
                                        set.Add(cacheKey);
                                        return set;
                                    });
            }

            _valuesCacheManager.Add(cacheKey, value);
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
                _dependenciesCacheManager.Remove(rootCacheKey);
            }
        }

        private void clearDependencyValues(string rootCacheKey)
        {
            var dependencyKeys = _dependenciesCacheManager.Get(rootCacheKey);
            if (dependencyKeys == null)
            {
                return;
            }

            foreach (var dependencyKey in dependencyKeys)
            {
                _valuesCacheManager.Remove(dependencyKey);
            }
        }
    }
}