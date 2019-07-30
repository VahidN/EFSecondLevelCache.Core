#if NETSTANDARD2_1
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using EFSecondLevelCache.Core.Contracts;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Getting the SQL for a Query
    /// </summary>
    public static class EFQueryCompilerExtensions3x
    {
        private static readonly TypeInfo _queryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();

        private static readonly FieldInfo _queryCompilerField =
            typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");

        private static readonly FieldInfo _queryContextFactoryField =
            _queryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryContextFactory");

        private static readonly FieldInfo _loggerField =
            _queryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_logger");

        /// <summary>
        /// Getting the SQL for a Query
        /// </summary>
        /// <param name="query">The query</param>
        /// <param name="expression">The expression tree</param>
        /// <param name="cacheKeyHashProvider">The CacheKey Hash Provider</param>
        public static EFToSqlData ToSql<TEntity>(
            this IQueryable<TEntity> query,
            Expression expression,
            IEFCacheKeyHashProvider cacheKeyHashProvider)
        {
            var queryCompiler = (QueryCompiler) _queryCompilerField.GetValue(query.Provider);
            var expressionKeyHash = getExpressionKeyHash(queryCompiler, cacheKeyHashProvider, expression);
            return new EFToSqlData(string.Empty, expressionKeyHash);
        }

        private static string getExpressionKeyHash(
            QueryCompiler queryCompiler,
            IEFCacheKeyHashProvider cacheKeyHashProvider,
            Expression expression)
        {
            var queryContextFactory = (IQueryContextFactory) _queryContextFactoryField.GetValue(queryCompiler);
            var queryContext = queryContextFactory.Create();
            var logger = (IDiagnosticsLogger<DbLoggerCategory.Query>) _loggerField.GetValue(queryCompiler);
            expression = queryCompiler.ExtractParameters(expression, queryContext, logger);

            var expressionKey = $"{ExpressionEqualityComparer.Instance.GetHashCode(expression)};";
            var parameterValues = queryContext.ParameterValues;
            if (parameterValues.Any())
            {
                expressionKey = parameterValues.Aggregate(expressionKey,
                    (current, item) => current + $"{item.Key}={item.Value?.GetHashCode()};");
            }

            return cacheKeyHashProvider.ComputeHash(expressionKey);
        }
    }
}
#endif