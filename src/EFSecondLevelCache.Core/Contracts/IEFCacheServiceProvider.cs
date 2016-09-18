using System.Collections.Generic;

namespace EFSecondLevelCache.Core.Contracts
{
    /// <summary>
    /// Cache Service Provider Contract.
    /// </summary>
    public interface IEFCacheServiceProvider
    {
        /// <summary>
        /// Removes the cached entries added by this library.
        /// </summary>
        void ClearAllCachedEntries();

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="cacheKey">key to find</param>
        /// <returns>cached value</returns>
        object GetValue(string cacheKey);

        /// <summary>
        /// Adds a new item to the cache.
        /// </summary>
        /// <param name="cacheKey">key</param>
        /// <param name="value">value</param>
        /// <param name="rootCacheKeys">cache dependencies</param>
        void InsertValue(string cacheKey, object value, ISet<string> rootCacheKeys);

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="rootCacheKeys">cache dependencies</param>
        void InvalidateCacheDependencies(string[] rootCacheKeys);
    }
}