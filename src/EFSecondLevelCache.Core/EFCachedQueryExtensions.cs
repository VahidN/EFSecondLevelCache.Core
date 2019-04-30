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
        private static IEFCacheKeyProvider _defaultCacheKeyProvider;
        private static IEFCacheServiceProvider _defaultCacheServiceProvider;

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        /// <returns></returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, string saltKey, EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider, IEFCacheServiceProvider cacheServiceProvider)
        {
            return new EFCachedQueryable<TType>(query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(
           this IQueryable query, string saltKey, EFCacheDebugInfo debugInfo,
           IEFCacheKeyProvider cacheKeyProvider, IEFCacheServiceProvider cacheServiceProvider)
        {
            var type = typeof(EFCachedQueryable<>).MakeGenericType(query.ElementType);
            var cachedQueryable = Activator.CreateInstance(type, query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
            return cachedQueryable as IQueryable;
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        /// <returns></returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, string saltKey, EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider, IEFCacheServiceProvider cacheServiceProvider) where TType : class
        {
            return new EFCachedDbSet<TType>(query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, string saltKey, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider)
        {
            var cacheServiceProvider = serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
            var cacheKeyProvider = serviceProvider.GetRequiredService<IEFCacheKeyProvider>();
            return new EFCachedQueryable<TType>(query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static IQueryable Cacheable(
            this IQueryable query, string saltKey, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider)
        {
            var cacheServiceProvider = serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
            var cacheKeyProvider = serviceProvider.GetRequiredService<IEFCacheKeyProvider>();
            return Cacheable(query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        /// <returns></returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, string saltKey, EFCacheDebugInfo debugInfo, IServiceProvider serviceProvider) where TType : class
        {
            var cacheServiceProvider = serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
            var cacheKeyProvider = serviceProvider.GetRequiredService<IEFCacheKeyProvider>();
            return new EFCachedDbSet<TType>(query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
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
            return Cacheable(query, string.Empty, debugInfo, serviceProvider);
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
            return Cacheable(query, string.Empty, debugInfo, serviceProvider);
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
            return Cacheable(query, string.Empty, debugInfo, serviceProvider);
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
            return Cacheable(query, string.Empty, new EFCacheDebugInfo(), serviceProvider);
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
            return Cacheable(query, string.Empty, new EFCacheDebugInfo(), serviceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, string saltKey, EFCacheDebugInfo debugInfo)
        {
            configureProviders();
            return Cacheable(query, saltKey, debugInfo, _defaultCacheKeyProvider, _defaultCacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, string saltKey, EFCacheDebugInfo debugInfo) where TType : class
        {
            configureProviders();
            return Cacheable(query, saltKey, debugInfo, _defaultCacheKeyProvider, _defaultCacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(this IQueryable query, string saltKey, EFCacheDebugInfo debugInfo)
        {
            configureProviders();
            return Cacheable(query, saltKey, debugInfo, _defaultCacheKeyProvider, _defaultCacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(this IQueryable<TType> query)
        {
            return Cacheable(query, string.Empty, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(this IQueryable query)
        {
            return Cacheable(query, string.Empty, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(this DbSet<TType> query) where TType : class
        {
            return Cacheable(query, string.Empty, new EFCacheDebugInfo());
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
            return Cacheable(query, string.Empty, debugInfo);
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
            return Cacheable(query, string.Empty, debugInfo);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(this IQueryable<TType> query, string saltKey)
        {
            return Cacheable(query, saltKey, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static IQueryable Cacheable(this IQueryable query, string saltKey)
        {
            return Cacheable(query, saltKey, new EFCacheDebugInfo());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedDbSet<TType> Cacheable<TType>(
            this DbSet<TType> query, string saltKey) where TType : class
        {
            return Cacheable(query, saltKey, new EFCacheDebugInfo());
        }

        private static void configureProviders()
        {
            if (_defaultCacheServiceProvider != null && _defaultCacheKeyProvider != null)
            {
                return;
            }

            var applicationServices = EFServiceCollectionExtensions.ServiceCollection?.BuildServiceProvider();
            if (applicationServices == null)
            {
                throw new InvalidOperationException("Please add `AddEFSecondLevelCache()` method to your `IServiceCollection`.");
            }

            _defaultCacheServiceProvider = applicationServices.GetRequiredService<IEFCacheServiceProvider>();
            _defaultCacheKeyProvider = applicationServices.GetRequiredService<IEFCacheKeyProvider>();
        }
    }
}