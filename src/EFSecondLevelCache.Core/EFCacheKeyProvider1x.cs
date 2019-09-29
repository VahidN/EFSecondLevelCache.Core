#if !NETSTANDARD2_0 && !NET4_6_1 && !NETSTANDARD2_1
using System.Linq;
using System.Linq.Expressions;
using EFSecondLevelCache.Core.Contracts;
using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.Structure;
using Microsoft.EntityFrameworkCore.Internal;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// A custom cache key provider for EF queries.
    /// </summary>
    public class EFCacheKeyProvider : IEFCacheKeyProvider
    {
        private static readonly TypeInfo _queryCompilerTypeInfo =
            typeof(QueryCompiler).GetTypeInfo();
        private static readonly FieldInfo _queryCompilerField =
            typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");
        private static readonly PropertyInfo _nodeTypeProviderField =
            _queryCompilerTypeInfo.DeclaredProperties.Single(x => x.Name == "NodeTypeProvider");
        private static readonly MethodInfo _createQueryParserMethod =
            _queryCompilerTypeInfo.DeclaredMethods.First(x => x.Name == "CreateQueryParser");
        private static readonly FieldInfo _dataBaseField =
            _queryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");
        private static readonly FieldInfo _queryCompilationContextFactoryField =
            typeof(Database).GetTypeInfo().DeclaredFields.Single(x => x.Name == "_queryCompilationContextFactory");

        private readonly IEFCacheKeyHashProvider _cacheKeyHashProvider;

        /// <summary>
        /// A custom cache key provider for EF queries.
        /// </summary>
        /// <param name="cacheKeyHashProvider">Provides the custom hashing algorithm.</param>
        public EFCacheKeyProvider(IEFCacheKeyHashProvider cacheKeyHashProvider)
        {
            _cacheKeyHashProvider = cacheKeyHashProvider;
        }

        /// <summary>
        /// Gets an EF query and returns its hashed key to store in the cache.
        /// </summary>
        /// <param name="query">The EF query.</param>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <returns>Information of the computed key of the input LINQ query.</returns>
        public EFCacheKey GetEFCacheKey(IQueryable query, Expression expression, string saltKey = "")
        {
            var expressionVisitorResult = EFQueryExpressionVisitor.GetDebugView(expression);
            var sqlData = toSql(query, expression, _cacheKeyHashProvider);
            var key = $"{sqlData};{expressionVisitorResult.DebugView};{saltKey}";
            var keyHash = _cacheKeyHashProvider.ComputeHash(key);
            return new EFCacheKey
            {
                Key = key,
                KeyHash = keyHash,
                CacheDependencies = expressionVisitorResult.Types
            };
        }

        private static string toSql(
            IQueryable query,
            Expression expression,
            IEFCacheKeyHashProvider cacheKeyHashProvider)
        {
            var queryCompiler = (IQueryCompiler)_queryCompilerField.GetValue(query.Provider);
            var nodeTypeProvider = (INodeTypeProvider)_nodeTypeProviderField.GetValue(queryCompiler);
            var parser = (IQueryParser)_createQueryParserMethod.Invoke(queryCompiler, new object[] { nodeTypeProvider });
            var queryModel = parser.GetParsedQuery(expression);
            var database = _dataBaseField.GetValue(queryCompiler);
            var queryCompilationContextFactory = (IQueryCompilationContextFactory)_queryCompilationContextFactoryField.GetValue(database);
            var queryCompilationContext = queryCompilationContextFactory.Create(false);
            var modelVisitor = queryCompilationContext.CreateQueryModelVisitor();

            try
            {
                var createQueryExecutorMethod = modelVisitor.GetType().GetMethod("CreateQueryExecutor").MakeGenericMethod(query.ElementType);
                createQueryExecutorMethod.Invoke(modelVisitor, new object[] { queryModel });
            }
            catch (ArgumentException)
            {
                // we don't care about its final casting and result.
            }

            if (!(modelVisitor is RelationalQueryModelVisitor relationalQueryModelVisitor))
            {
                return queryModel.ToString();
            }

            return relationalQueryModelVisitor.Queries.Join(Environment.NewLine);
        }
    }
}
#endif