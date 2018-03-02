using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.Structure;

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

#if NETSTANDARD2_0 || NET4_6_1
        private static readonly PropertyInfo _databaseDependenciesProperty =
            typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");
#else
        private static readonly FieldInfo _queryCompilationContextFactoryField =
            typeof(Database).GetTypeInfo().DeclaredFields.Single(x => x.Name == "_queryCompilationContextFactory");
#endif

        /// <summary>
        /// Getting the SQL for a Query
        /// </summary>
        /// <param name="query">The query</param>
        public static string ToSql<TEntity>(this IQueryable<TEntity> query)
        {
            var queryCompiler = (IQueryCompiler)_queryCompilerField.GetValue(query.Provider);
            var queryModelGenerator = _queryModelGeneratorField.GetValue(queryCompiler);
            var nodeTypeProvider = (INodeTypeProvider)_nodeTypeProviderField.GetValue(queryModelGenerator);
            var parser = (IQueryParser)_createQueryParserMethod.Invoke(queryModelGenerator, new object[] { nodeTypeProvider });
            var queryModel = parser.GetParsedQuery(query.Expression);
            var database = _dataBaseField.GetValue(queryCompiler);

#if NETSTANDARD2_0 || NET4_6_1
            var databaseDependencies = (DatabaseDependencies)_databaseDependenciesProperty.GetValue(database);
            var queryCompilationContextFactory = databaseDependencies.QueryCompilationContextFactory;
#else
            var queryCompilationContextFactory = (IQueryCompilationContextFactory)_queryCompilationContextFactoryField.GetValue(database);
#endif            

            var queryCompilationContext = queryCompilationContextFactory.Create(false);
            var modelVisitor = queryCompilationContext.CreateQueryModelVisitor();
            modelVisitor.CreateQueryExecutor<TEntity>(queryModel);

            var relationalQueryModelVisitor = modelVisitor as RelationalQueryModelVisitor;
            if (relationalQueryModelVisitor == null)
            {
                return queryModel.ToString();
            }

            var sql = relationalQueryModelVisitor.Queries.First().ToString();
            return sql;
        }
    }
}