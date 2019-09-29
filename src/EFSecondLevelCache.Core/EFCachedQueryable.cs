using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Provides functionality to evaluate queries against a specific data source.
    /// </summary>
    /// <typeparam name="TType">Type of the entity.</typeparam>
    public class EFCachedQueryable<TType> : IQueryable<TType>
#if !NETSTANDARD2_1
        , IAsyncEnumerableAccessor<TType>
#else
        , IAsyncEnumerable<TType>
#endif
    {
        private readonly EFCachedQueryProvider _provider;

        /// <summary>
        /// Provides functionality to evaluate queries against a specific data source.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        public EFCachedQueryable(
            IQueryable<TType> query,
            EFCachePolicy cachePolicy,
            EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider,
            IEFCacheServiceProvider cacheServiceProvider)
        {
            CachePolicy = cachePolicy;
            DebugInfo = debugInfo;
            CacheKeyProvider = cacheKeyProvider;
            CacheServiceProvider = cacheServiceProvider;
            Query = query.MarkAsNoTracking();
            _provider = new EFCachedQueryProvider(Query, cachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Asynchronous version of the IEnumerable interface
        /// </summary>
        public IAsyncEnumerable<TType> AsyncEnumerable => new EFAsyncEnumerable<TType>(this.AsEnumerable().GetEnumerator());

        /// <summary>
        /// Gets an EF query and returns its hash to store in the cache.
        /// </summary>
        public IEFCacheKeyProvider CacheKeyProvider { get; }

        /// <summary>
        /// Cache Service Provider.
        /// </summary>
        public IEFCacheServiceProvider CacheServiceProvider { get; }

        /// <summary>
        /// Stores the debug information of the caching process.
        /// </summary>
        public EFCacheDebugInfo DebugInfo { get; }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of System.Linq.IQueryable is executed.
        /// </summary>
        public Type ElementType => Query.ElementType;

        /// <summary>
        /// Gets the expression tree that is associated with the instance of System.Linq.IQueryable.
        /// </summary>
        public Expression Expression => Query.Expression;

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        public IQueryProvider Provider => _provider;

        /// <summary>
        /// The input EF query.
        /// </summary>
        public IQueryable<TType> Query { get; }

        /// <summary>
        /// Defines the expiration mode of the cache item.
        /// </summary>
        public EFCachePolicy CachePolicy { get; }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_provider.Materializer.Materialize(
                               Query.Expression, 
                               () => Query.ToArray())).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        IEnumerator<TType> IEnumerable<TType>.GetEnumerator()
        {
            return ((IEnumerable<TType>)_provider.Materializer.Materialize(
                                   Query.Expression, 
                                   () => Query.ToArray())).GetEnumerator();
        }

#if NETSTANDARD2_1
        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        public IAsyncEnumerator<TType> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new EFAsyncEnumerator<TType>(
                     ((IEnumerable<TType>)_provider.Materializer.Materialize(
                                        Query.Expression, 
                                        () => Query.ToArray())).GetEnumerator());
        }
#endif
    }
}