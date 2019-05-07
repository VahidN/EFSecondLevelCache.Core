using System;
using System.Linq;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Returns a new cached query.
    /// </summary>
    public static class EFCachedQueryExtensions
    {
        private static readonly IEFCacheKeyProvider _defaultCacheKeyProvider;
        private static readonly IEFCacheServiceProvider _defaultCacheServiceProvider;

        static EFCachedQueryExtensions()
        {
            var serviceProvider = EFStaticServiceProvider.Instance;
            _defaultCacheServiceProvider = serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
            _defaultCacheKeyProvider = serviceProvider.GetRequiredService<IEFCacheKeyProvider>();
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        /// <returns></returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider, IEFCacheServiceProvider cacheServiceProvider)
        {
            return new EFCachedQueryable<TType>(query, cachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(
           this IQueryable query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo,
           IEFCacheKeyProvider cacheKeyProvider, IEFCacheServiceProvider cacheServiceProvider)
        {
            var type = typeof(EFCachedQueryable<>).MakeGenericType(query.ElementType);
            var cachedQueryable = Activator.CreateInstance(type, query, cachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
            return cachedQueryable as IQueryable;
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        /// <returns></returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider, IEFCacheServiceProvider cacheServiceProvider) where TType : class
        {
            return new EFCachedDbSet<TType>(query, cachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider)
        {
            var cacheServiceProvider = serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
            var cacheKeyProvider = serviceProvider.GetRequiredService<IEFCacheKeyProvider>();
            return new EFCachedQueryable<TType>(query, cachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static IQueryable Cacheable(
            this IQueryable query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider)
        {
            var cacheServiceProvider = serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
            var cacheKeyProvider = serviceProvider.GetRequiredService<IEFCacheKeyProvider>();
            return Cacheable(query, cachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider) where TType : class
        {
            var cacheServiceProvider = serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
            var cacheKeyProvider = serviceProvider.GetRequiredService<IEFCacheKeyProvider>();
            return new EFCachedDbSet<TType>(query, cachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider)
        {
            return Cacheable(query, null, debugInfo, serviceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static IQueryable Cacheable(
            this IQueryable query, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider)
        {
            return Cacheable(query, null, debugInfo, serviceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider) where TType : class
        {
            return Cacheable(query, null, debugInfo, serviceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, IServiceProvider serviceProvider)
        {
            return Cacheable(query, null, new EFCacheDebugInfo(), serviceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, IServiceProvider serviceProvider) where TType : class
        {
            return Cacheable(query, null, new EFCacheDebugInfo(), serviceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo)
        {
            return Cacheable(query, cachePolicy, debugInfo, _defaultCacheKeyProvider, _defaultCacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo) where TType : class
        {
            return Cacheable(query, cachePolicy, debugInfo, _defaultCacheKeyProvider, _defaultCacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(this IQueryable query, EFCachePolicy cachePolicy, EFCacheDebugInfo debugInfo)
        {
            return Cacheable(query, cachePolicy, debugInfo, _defaultCacheKeyProvider, _defaultCacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(this IQueryable<TType> query)
        {
            return Cacheable(query, null, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(this IQueryable query)
        {
            return Cacheable(query, null, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(this DbSet<TType> query) where TType : class
        {
            return Cacheable(query, null, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(this IQueryable<TType> query, EFCacheDebugInfo debugInfo)
        {
            return Cacheable(query, null, debugInfo);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(this DbSet<TType> query, EFCacheDebugInfo debugInfo) where TType : class
        {
            return Cacheable(query, null, debugInfo);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(this IQueryable<TType> query, EFCachePolicy cachePolicy)
        {
            return Cacheable(query, cachePolicy, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="expirationMode">Defines the expiration mode of the cache item.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, CacheExpirationMode expirationMode, TimeSpan timeout)
        {
            return Cacheable(query, new EFCachePolicy(expirationMode, timeout), new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="expirationMode">Defines the expiration mode of the cache item.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, CacheExpirationMode expirationMode, TimeSpan timeout, EFCacheDebugInfo debugInfo)
        {
            return Cacheable(query, new EFCachePolicy(expirationMode, timeout), debugInfo);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(this IQueryable query, EFCachePolicy cachePolicy)
        {
            return Cacheable(query, cachePolicy, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item. If you set it to null or don't specify it, the global `new CacheManager.Core.ConfigurationBuilder().WithExpiration()` setting will be used automatically.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, EFCachePolicy cachePolicy) where TType : class
        {
            return Cacheable(query, cachePolicy, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="expirationMode">Defines the expiration mode of the cache item.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, CacheExpirationMode expirationMode, TimeSpan timeout) where TType : class
        {
            return Cacheable(query, new EFCachePolicy(expirationMode, timeout), new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="expirationMode">Defines the expiration mode of the cache item.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, CacheExpirationMode expirationMode, TimeSpan timeout, EFCacheDebugInfo debugInfo) where TType : class
        {
            return Cacheable(query, new EFCachePolicy(expirationMode, timeout), debugInfo);
        }
    }
}