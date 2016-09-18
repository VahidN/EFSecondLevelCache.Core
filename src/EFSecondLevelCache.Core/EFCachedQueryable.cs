using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Provides functionality to evaluate queries against a specific data source.
    /// </summary>
    /// <typeparam name="TType">Type of the entity.</typeparam>
    public class EFCachedQueryable<TType> : IQueryable<TType>, IAsyncEnumerableAccessor<TType>
    {
        private static readonly MethodInfo _asNoTrackingMethodInfo =
            typeof(EntityFrameworkQueryableExtensions).GetTypeInfo().GetDeclaredMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking));

        private readonly EFCachedQueryProvider<TType> _provider;
        private readonly IQueryable<TType> _query;

        /// <summary>
        /// Provides functionality to evaluate queries against a specific data source.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        public EFCachedQueryable(
            IQueryable<TType> query,
            string saltKey,
            EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider,
            IEFCacheServiceProvider cacheServiceProvider)
        {
            _query = markAsNoTracking(query);
            _provider = new EFCachedQueryProvider<TType>(_query, saltKey, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        ///
        /// </summary>
        public IAsyncEnumerable<TType> AsyncEnumerable => new EFAsyncEnumerable<TType>(this.AsEnumerable().GetEnumerator());

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of System.Linq.IQueryable is executed.
        /// </summary>
        public Type ElementType => _query.ElementType;

        /// <summary>
        /// Gets the expression tree that is associated with the instance of System.Linq.IQueryable.
        /// </summary>
        public Expression Expression => _query.Expression;

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        public IQueryProvider Provider => _provider;

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_provider.Materialize(_query.Expression, () => _query.ToArray())).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        IEnumerator<TType> IEnumerable<TType>.GetEnumerator()
        {
            return ((IEnumerable<TType>)_provider.Materialize(_query.Expression, () => _query.ToArray())).GetEnumerator();
        }

        private static IQueryable<TType> markAsNoTracking(IQueryable<TType> query)
        {
            if (typeof(TType).GetTypeInfo().IsClass)
            {
                return query.Provider.CreateQuery<TType>(
                        Expression.Call(null, _asNoTrackingMethodInfo.MakeGenericMethod(typeof(TType)), query.Expression));
            }
            return query;
        }
    }
}