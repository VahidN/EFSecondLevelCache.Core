using System;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Defines the supported expiration modes for cache items.
    /// </summary>
    public enum CacheExpirationMode
    {
        /// <summary>
        /// Defines absolute expiration. The item will expire after the expiration timeout.
        /// </summary>
        Absolute,

        /// <summary>
        /// Defines sliding expiration. The expiration timeout will be refreshed on every access.
        /// </summary>
        Sliding
    }

    /// <summary>
    /// EFCachePolicy determines the Expiration time of the cache.
    /// If you don't define it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.
    /// </summary>
    public class EFCachePolicy
    {
        /// <summary>
        /// Defines the expiration mode of the cache item.
        /// Its deafult value is Absolute.
        /// </summary>
        public CacheExpirationMode ExpirationMode { set; get; }

        /// <summary>
        /// The expiration timeout.
        /// Its deafult value is 20 minutes later.
        /// </summary>
        /// <value></value>
        public TimeSpan Timeout { set; get; } = TimeSpan.FromMinutes(20);

        /// <summary>
        /// If you think the computed hash of the query to calculate the cache-key is not enough, set this value.
        /// Its deafult value is string.Empty.
        /// </summary>
        public string SaltKey { set; get; } = string.Empty;

        /// <summary>
        /// EFCachePolicy determines the Expiration time of the cache.
        /// </summary>
        public EFCachePolicy() { }

        /// <summary>
        /// EFCachePolicy determines the Expiration time of the cache.
        /// </summary>
        /// <param name="expirationMode">Defines the expiration mode of the cache item.</param>
        /// <param name="timeout">The expiration timeout.</param>
        public EFCachePolicy(CacheExpirationMode expirationMode, TimeSpan timeout)
        {
            ExpirationMode = expirationMode;
            Timeout = timeout;
        }

        /// <summary>
        /// EFCachePolicy determines the Expiration time of the cache.
        /// </summary>
        /// <param name="expirationMode">Defines the expiration mode of the cache item.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <param name="saltKey">If you think the computed hash of the query to calculate the cache-key is not enough, set this value.</param>
        public EFCachePolicy(CacheExpirationMode expirationMode, TimeSpan timeout, string saltKey)
        {
            ExpirationMode = expirationMode;
            Timeout = timeout;
            SaltKey = saltKey;
        }
    }
}