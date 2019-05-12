#if NETSTANDARD2_0 || NET4_6_1
using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.Structure;
using System.Linq.Expressions;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Getting the SQL for a Query
    /// </summary>
    public static class EFQueryCompilerExtensions
    {
        private static readonly TypeInfo _queryCompilerTypeInfo =
                    typeof(QueryCompiler).GetTypeInfo();
        private static readonly FieldInfo _queryCompilerField =
            typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");
        private static readonly TypeInfo _gueryModelGeneratorInfo =
            typeof(QueryModelGenerator).GetTypeInfo();
        private static readonly FieldInfo _queryModelGeneratorField =
            _queryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryModelGenerator");
        private static readonly FieldInfo _nodeTypeProviderField =
            _gueryModelGeneratorInfo.DeclaredFields.Single(x => x.Name == "_nodeTypeProvider");
        private static readonly MethodInfo _createQueryParserMethod =
            _gueryModelGeneratorInfo.DeclaredMethods.First(x => x.Name == "CreateQueryParser");
        private static readonly FieldInfo _dataBaseField =
            _queryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");
        private static readonly PropertyInfo _databaseDependenciesProperty =
            typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");

        /// <summary>
        /// Getting the SQL for a Query
        /// </summary>
        /// <param name="query">The query</param>
        /// <param name="expression">The expressin tree</param>
        public static string ToSql<TEntity>(this IQueryable<TEntity> query, Expression expression)
        {
            var queryCompiler = (IQueryCompiler)_queryCompilerField.GetValue(query.Provider);
            var queryModelGenerator = _queryModelGeneratorField.GetValue(queryCompiler);
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
                return queryModel.ToString();
            }

            var sql = relationalQueryModelVisitor.Queries.First().ToString();
            return sql;
        }
    }
}
#endif