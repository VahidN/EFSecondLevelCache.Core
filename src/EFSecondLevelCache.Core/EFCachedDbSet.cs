using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Provides functionality to evaluate queries against a specific data source.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public class EFCachedDbSet<TType> : IQueryable<TType>, IAsyncEnumerableAccessor<TType> where TType : class
    {
        private readonly EFCachedQueryProvider<TType> _provider;

        /// <summary>
        /// Provides functionality to evaluate queries against a specific data source.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        public EFCachedDbSet(
            DbSet<TType> query,
            string saltKey,
            EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider,
            IEFCacheServiceProvider cacheServiceProvider)
        {
            SaltKey = saltKey;
            DebugInfo = debugInfo;
            CacheKeyProvider = cacheKeyProvider;
            CacheServiceProvider = cacheServiceProvider;
            Query = query;
            _provider = new EFCachedQueryProvider<TType>(Query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
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
        public Type ElementType => Query.AsQueryable().ElementType;

        /// <summary>
        /// Gets the expression tree that is associated with the instance of System.Linq.IQueryable.
        /// </summary>
        public Expression Expression => Query.AsQueryable().Expression;

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        public IQueryProvider Provider => _provider;

        /// <summary>
        /// The input EF query.
        /// </summary>
        public DbSet<TType> Query { get; }

        /// <summary>
        /// If you think the computed hash of the query is not enough, set this value.
        /// </summary>
        public string SaltKey { get; }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_provider.Materialize(Query.AsQueryable().Expression, () => Query.ToArray())).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        IEnumerator<TType> IEnumerable<TType>.GetEnumerator()
        {
            return ((IEnumerable<TType>)_provider.Materialize(Query.AsQueryable().Expression, () => Query.ToArray())).GetEnumerator();
        }
    }
}