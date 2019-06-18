#if NETSTANDARD2_0 || NET4_6_1
using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.Structure;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;
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
        private static readonly TypeInfo _queryCompilerTypeInfo =
                    typeof(QueryCompiler).GetTypeInfo();
        private static readonly FieldInfo _queryCompilerField =
            typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");
        private static readonly TypeInfo _queryModelGeneratorInfo =
            typeof(QueryModelGenerator).GetTypeInfo();
        private static readonly FieldInfo _queryModelGeneratorField =
            _queryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryModelGenerator");
        private static readonly FieldInfo _nodeTypeProviderField =
            _queryModelGeneratorInfo.DeclaredFields.Single(x => x.Name == "_nodeTypeProvider");
        private static readonly MethodInfo _createQueryParserMethod =
            _queryModelGeneratorInfo.DeclaredMethods.First(x => x.Name == "CreateQueryParser");
        private static readonly FieldInfo _dataBaseField =
            _queryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");
        private static readonly PropertyInfo _databaseDependenciesProperty =
            typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");
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
        public static EFToSqlData ToSql<TEntity>(
            this IQueryable<TEntity> query,
            Expression expression,
            IEFCacheKeyHashProvider cacheKeyHashProvider)
        {
            var queryCompiler = (IQueryCompiler)_queryCompilerField.GetValue(query.Provider);
            var queryModelGenerator = (IQueryModelGenerator)_queryModelGeneratorField.GetValue(queryCompiler);

            var expressionKeyHash = getExpressionKeyHash(queryCompiler, queryModelGenerator, cacheKeyHashProvider, expression);
            var cachedSql = _keysCacheManager.Get<string>(expressionKeyHash);
            if (cachedSql != null)
            {
                return new EFToSqlData(cachedSql, expressionKeyHash);
            }

            var nodeTypeProvider = (INodeTypeProvider)_nodeTypeProviderField.GetValue(queryModelGenerator);
            var parser = (IQueryParser)_createQueryParserMethod.Invoke(queryModelGenerator, new object[] { nodeTypeProvider });
            var queryModel = parser.GetParsedQuery(expression);
            var database = _dataBaseField.GetValue(queryCompiler);
            var databaseDependencies = (DatabaseDependencies)_databaseDependenciesProperty.GetValue(database);
            var queryCompilationContextFactory = databaseDependencies.QueryCompilationContextFactory;
            var queryCompilationContext = queryCompilationContextFactory.Create(false);
            var modelVisitor = queryCompilationContext.CreateQueryModelVisitor();

            try
            {
                modelVisitor.CreateQueryExecutor<TEntity>(queryModel);
            }
            catch (ArgumentException)
            {
                // we don't care about its final casting and result.
            }

            if (!(modelVisitor is RelationalQueryModelVisitor relationalQueryModelVisitor))
            {
                var queryModelInfo = queryModel.ToString();
                setCache(expressionKeyHash, queryModelInfo);
                return new EFToSqlData(queryModelInfo, expressionKeyHash);
            }

            var sql = relationalQueryModelVisitor.Queries.Join(Environment.NewLine);
            setCache(expressionKeyHash, sql);
            return new EFToSqlData(sql, expressionKeyHash);
        }

        private static void setCache(string expressionKeyHash, string sql)
        {
            _keysCacheManager.Add(
                new CacheItem<string>(expressionKeyHash, sql, ExpirationMode.Sliding, _slidingExpirationTimeSpan));
        }

        private static string getExpressionKeyHash(
            IQueryCompiler queryCompiler,
            IQueryModelGenerator queryModelGenerator,
            IEFCacheKeyHashProvider cacheKeyHashProvider,
            Expression expression)
        {
            var queryContextFactory = (IQueryContextFactory)_queryContextFactoryField.GetValue(queryCompiler);
            var queryContext = queryContextFactory.Create();
            var logger = (IDiagnosticsLogger<DbLoggerCategory.Query>)_loggerField.GetValue(queryCompiler);
            expression = queryModelGenerator.ExtractParameters(logger, expression, queryContext);

            var expressionKey = $"{ExpressionEqualityComparer.Instance.GetHashCode(expression)};";
            var parameterValues = queryContext.ParameterValues;
            if (parameterValues.Any())
            {
                expressionKey = parameterValues.Aggregate(expressionKey, (current, item) => current + $"{item.Key}={item.Value?.GetHashCode()};");
            }
            return cacheKeyHashProvider.ComputeHash(expressionKey);
        }
    }
}
#endif