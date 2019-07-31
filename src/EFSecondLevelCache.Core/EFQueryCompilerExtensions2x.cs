#if NETSTANDARD2_0 || NET4_6_1
using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using EFSecondLevelCache.Core.Contracts;
using CacheManager.Core;
using Microsoft.Extensions.DependencyInjection;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Getting the SQL for a Query
    /// </summary>
    public static class EFQueryCompilerExtensions2x
    {
        private static readonly TypeInfo _queryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();

        private static readonly FieldInfo _queryCompilerField =
            typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");

        private static readonly FieldInfo _queryModelGeneratorField =
            _queryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryModelGenerator");

        private static readonly FieldInfo _queryContextFactoryField =
            _queryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryContextFactory");

        private static readonly FieldInfo _loggerField =
            _queryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_logger");

        private static readonly TimeSpan _slidingExpirationTimeSpan = TimeSpan.FromMinutes(7);

        private static readonly ICacheManager<string> _keysCacheManager =
            EFStaticServiceProvider.Instance.GetRequiredService<ICacheManager<string>>();

        /// <summary>
        /// Getting the SQL for a Query
        /// </summary>
        /// <param name="query">The query</param>
        /// <param name="expression">The expression tree</param>
        /// <param name="cacheKeyHashProvider">The CacheKey Hash Provider</param>
        public static string ToSql<TEntity>(
            this IQueryable<TEntity> query,
            Expression expression,
            IEFCacheKeyHashProvider cacheKeyHashProvider)
        {
            var queryCompiler = (QueryCompiler) _queryCompilerField.GetValue(query.Provider);
            var queryModelGenerator = (IQueryModelGenerator) _queryModelGeneratorField.GetValue(queryCompiler);

            var (expressionKeyHash, modifiedExpression) =
                getExpressionKeyHash(queryCompiler, queryModelGenerator, cacheKeyHashProvider, expression);
            var cachedSql = _keysCacheManager.Get<string>(expressionKeyHash);
            if (cachedSql != null)
            {
                return cachedSql;
            }

            var expressionPrinter = new ExpressionPrinter();
            expressionPrinter.Visit(modifiedExpression);
            var sql = expressionPrinter.StringBuilder.ToString();
            setCache(expressionKeyHash, sql);
            return sql;
        }

        private static void setCache(string expressionKeyHash, string sql)
        {
            _keysCacheManager.Add(
                new CacheItem<string>(expressionKeyHash, sql, ExpirationMode.Sliding, _slidingExpirationTimeSpan));
        }

        private static (string ExpressionKeyHash, Expression ModifiedExpression) getExpressionKeyHash(
            QueryCompiler queryCompiler,
            IQueryModelGenerator queryModelGenerator,
            IEFCacheKeyHashProvider cacheKeyHashProvider,
            Expression expression)
        {
            var queryContextFactory = (IQueryContextFactory) _queryContextFactoryField.GetValue(queryCompiler);
            var queryContext = queryContextFactory.Create();
            var logger = (IDiagnosticsLogger<DbLoggerCategory.Query>) _loggerField.GetValue(queryCompiler);
            expression = queryModelGenerator.ExtractParameters(logger, expression, queryContext, parameterize: false);

            var expressionKey = $"{ExpressionEqualityComparer.Instance.GetHashCode(expression)};";
            var parameterValues = queryContext.ParameterValues;
            if (parameterValues.Any())
            {
                expressionKey = parameterValues.Aggregate(expressionKey,
                    (current, item) => current + $"{item.Key}={item.Value?.GetHashCode()};");
            }

            return (cacheKeyHashProvider.ComputeHash(expressionKey), expression);
        }
    }
}
#endif