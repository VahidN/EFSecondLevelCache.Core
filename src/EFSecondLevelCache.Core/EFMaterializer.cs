using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Core.Contracts;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Cache Result Container
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheResult<T>
    {
        /// <summary>
        /// Could read from the cache?
        /// </summary>
        public bool CanRead { set; get; }

        /// <summary>
        /// EFCacheKey value
        /// </summary>
        public EFCacheKey CacheKey { set; get; }

        /// <summary>
        ///  Retrieved result from the cache
        /// </summary>
        public T Result { set; get; }

        /// <summary>
        /// Cache Result Container
        /// </summary>
        public CacheResult()
        {
        }

        /// <summary>
        /// Cache Result Container
        /// </summary>
        /// <param name="canRead">Could read from the cache?</param>
        /// <param name="cacheKey">EFCacheKey value</param>
        /// <param name="result">Retrieved result from the cache</param>
        public CacheResult(bool canRead, EFCacheKey cacheKey, T result)
        {
            CanRead = canRead;
            CacheKey = cacheKey;
            Result = result;
        }
    }

    /// <summary>
    /// Defines methods to create and execute queries that are described by an System.Linq.IQueryable object.
    /// </summary>
    public class EFMaterializer
    {
        private readonly IEFCacheKeyProvider _cacheKeyProvider;
        private readonly IEFCacheServiceProvider _cacheServiceProvider;
        private readonly EFCacheDebugInfo _debugInfo;
        private readonly EFCachePolicy _cachePolicy;
        private readonly IQueryable _query;
        private static readonly object _syncLock = new object();
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);


        /// <summary>
        /// Defines methods to create and execute queries that are described by an System.Linq.IQueryable object.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">The Cache Service Provider.</param>
        public EFMaterializer(
            IQueryable query,
            EFCachePolicy cachePolicy,
            EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider,
            IEFCacheServiceProvider cacheServiceProvider)
        {
            _query = query;
            _cachePolicy = cachePolicy;
            _debugInfo = debugInfo;
            _cacheKeyProvider = cacheKeyProvider;
            _cacheServiceProvider = cacheServiceProvider;
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree to cache its results.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="materializer">How to run the query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public async Task<T> MaterializeAsync<T>(Expression expression, Func<Task<T>> materializer)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var cacheResult = readFromCache<T>(expression);
                if (cacheResult.CanRead)
                {
                    return cacheResult.Result;
                }

                var result = await materializer();
                _cacheServiceProvider.InsertValue(cacheResult.CacheKey.KeyHash, result,
                    cacheResult.CacheKey.CacheDependencies, _cachePolicy);
                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree to cache its results.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="materializer">How to run the query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public T Materialize<T>(Expression expression, Func<T> materializer)
        {
            lock (_syncLock)
            {
                var cacheResult = readFromCache<T>(expression);
                if (cacheResult.CanRead)
                {
                    return cacheResult.Result;
                }

                var result = materializer();
                _cacheServiceProvider.InsertValue(cacheResult.CacheKey.KeyHash, result,
                    cacheResult.CacheKey.CacheDependencies, _cachePolicy);
                return result;
            }
        }

        private CacheResult<T> readFromCache<T>(Expression expression)
        {
            var cacheKey = _cacheKeyProvider.GetEFCacheKey(_query, expression, _cachePolicy?.SaltKey);
            _debugInfo.EFCacheKey = cacheKey;
            var queryCacheKey = cacheKey.KeyHash;
            var result = _cacheServiceProvider.GetValue(queryCacheKey);
            if (Equals(result, _cacheServiceProvider.NullObject))
            {
                _debugInfo.IsCacheHit = true;
                return new CacheResult<T>(true, cacheKey, default);
            }

            if (result != null)
            {
                _debugInfo.IsCacheHit = true;
                return new CacheResult<T>(true, cacheKey, (T)result);
            }

            return new CacheResult<T>(false, cacheKey, default);
        }
    }
}