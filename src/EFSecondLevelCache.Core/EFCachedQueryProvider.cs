using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Defines methods to create and execute queries that are described by an System.Linq.IQueryable object.
    /// </summary>
    /// <typeparam name="TType">Type of the entity.</typeparam>
    public class EFCachedQueryProvider<TType> : IAsyncQueryProvider
    {
        private readonly IEFCacheKeyProvider _cacheKeyProvider;
        private readonly IEFCacheServiceProvider _cacheServiceProvider;
        private readonly EFCacheDebugInfo _debugInfo;
        private readonly EFCachePolicy _cachePolicy;
        private readonly IQueryable<TType> _query;
        private static readonly Object _syncLock = new Object();

#if NETSTANDARD2_1
        private static readonly MethodInfo _fromResultMethodInfo = typeof(Task).GetMethod("FromResult");
#endif

        /// <summary>
        /// Defines methods to create and execute queries that are described by an System.Linq.IQueryable object.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="cachePolicy">Defines the expiration mode of the cache item.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">The Cache Service Provider.</param>
        public EFCachedQueryProvider(
            IQueryable<TType> query,
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
        /// Constructs an System.Linq.IQueryable of T object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements that is returned.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An System.Linq.IQueryable of T that can evaluate the query represented by the specified expression tree.</returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return (IQueryable<TElement>)CreateQuery(expression);
        }

        /// <summary>
        /// Constructs an System.Linq.IQueryable object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An System.Linq.IQueryable that can evaluate the query represented by the specified expression tree.</returns>
        public IQueryable CreateQuery(Expression expression)
        {
            var argumentType = expression.Type.GenericTypeArguments[0];
            var cachedQueryable = typeof(EFCachedQueryable<>).MakeGenericType(argumentType);
            var constructorArgs = new object[]
            {
                _query.Provider.CreateQuery(expression),
                _cachePolicy,
                _debugInfo,
                _cacheKeyProvider,
                _cacheServiceProvider
            };
            return (IQueryable)Activator.CreateInstance(cachedQueryable, constructorArgs);
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public object Execute(Expression expression)
        {
            return Materialize(expression, () => _query.Provider.Execute(expression));
        }

        /// <summary>
        /// Executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Materialize(expression, () => _query.Provider.Execute<TResult>(expression));
        }

        /// <summary>
        /// This API supports the Entity Framework Core infrastructure
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            return new EFAsyncTaskEnumerable<TResult>(Task.FromResult(Execute<TResult>(expression)));
        }

        /// <summary>
        /// Asynchronously executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.  The task result contains the value that results from executing the specified query.</returns>
        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(expression));
        }

#if !NETSTANDARD2_1
        /// <summary>
        /// Asynchronously executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.  The task result contains the value that results from executing the specified query.</returns>
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<TResult>(expression));
        }
#else
        /// <summary>
        /// Asynchronously executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.  The task result contains the value that results from executing the specified query.</returns>
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            if (typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var result = Execute(expression);
                var taskFromResultMethod = _fromResultMethodInfo.MakeGenericMethod(
                                                result == null ? expression.Type : result.GetType());
                return (TResult)taskFromResultMethod.Invoke(null, new[] { result });
            }
            return Execute<TResult>(expression);
        }
#endif

        /// <summary>
        /// Executes the query represented by a specified expression tree to cache its results.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="materializer">How to run the query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public object Materialize(Expression expression, Func<object> materializer)
        {
            lock (_syncLock)
            {
                var cacheKey = _cacheKeyProvider.GetEFCacheKey(_query, expression, _cachePolicy?.SaltKey);
                _debugInfo.EFCacheKey = cacheKey;
                var queryCacheKey = cacheKey.KeyHash;
                var result = _cacheServiceProvider.GetValue(queryCacheKey);
                if (Equals(result, _cacheServiceProvider.NullObject))
                {
                    _debugInfo.IsCacheHit = true;
                    return null;
                }

                if (result != null)
                {
                    _debugInfo.IsCacheHit = true;
                    return result;
                }

                result = materializer();

                _cacheServiceProvider.InsertValue(queryCacheKey, result, cacheKey.CacheDependencies, _cachePolicy);

                return result;
            }
        }
    }
}