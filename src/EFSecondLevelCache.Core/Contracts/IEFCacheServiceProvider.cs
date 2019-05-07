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
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        void InsertValue(string cacheKey, object value, ISet<string> rootCacheKeys, EFCachePolicy cachePolicy);

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="rootCacheKeys">cache dependencies</param>
        void InvalidateCacheDependencies(string[] rootCacheKeys);

        /// <summary>
        /// Some cache providers won't accept null values.
        /// So we need a custom Null object here. It should be defined `static readonly` in your code.
        /// </summary>
        object NullObject { get; }
    }
}