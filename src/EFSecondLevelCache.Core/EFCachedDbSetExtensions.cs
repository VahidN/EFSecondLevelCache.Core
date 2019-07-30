using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

#if NETSTANDARD2_1
using Microsoft.EntityFrameworkCore.Diagnostics;
#endif

namespace EFSecondLevelCache.Core
{
    /// <summary>
    ///  DbSet Extensions
    /// </summary>
    public static class EFCachedDbSetExtensions
    {
        /// <summary>
        /// Finds an entity with the given primary key values.
        /// </summary>
        public static TEntity Find<TEntity>(
            this EFCachedDbSet<TEntity> cachedQueryable, params object[] keyValues)
            where TEntity : class
        {
            var query = buildFindQueryable(cachedQueryable, keyValues);
            return new EFCachedQueryable<TEntity>(
                query, cachedQueryable.CachePolicy, cachedQueryable.DebugInfo,
                cachedQueryable.CacheKeyProvider, cachedQueryable.CacheServiceProvider).FirstOrDefault();
        }

        /// <summary>
        /// Finds an entity with the given primary key values.
        /// </summary>
        public static Task<TEntity> FindAsync<TEntity>(
            this EFCachedDbSet<TEntity> cachedQueryable,
            object[] keyValues,
            CancellationToken cancellationToken) where TEntity : class
        {
            var query = buildFindQueryable(cachedQueryable, keyValues);
            return new EFCachedQueryable<TEntity>(
                query, cachedQueryable.CachePolicy, cachedQueryable.DebugInfo,
                cachedQueryable.CacheKeyProvider, cachedQueryable.CacheServiceProvider).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Finds an entity with the given primary key values.
        /// </summary>
        public static Task<TEntity> FindAsync<TEntity>(
            this EFCachedDbSet<TEntity> cachedQueryable,
            params object[] keyValues) where TEntity : class
        {
            return cachedQueryable.FindAsync(keyValues, default(CancellationToken));
        }

        private static IQueryable<TEntity> buildFindQueryable<TEntity>(
            EFCachedDbSet<TEntity> cachedQueryable, object[] keyValues)
            where TEntity : class
        {
            var set = cachedQueryable.Query;
            var context = set.GetInfrastructure().GetRequiredService<IDbContextServices>().CurrentContext.Context;
            var keyProperties = context.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;

            if (keyProperties.Count != keyValues.Length)
            {
                if (keyProperties.Count == 1)
                {
                    throw new ArgumentException(
                        CoreStrings.FindNotCompositeKey(typeof(TEntity).ShortDisplayName(), keyValues.Length));
                }
                throw new ArgumentException(
                    CoreStrings.FindValueCountMismatch(typeof(TEntity).ShortDisplayName(), keyProperties.Count,
                        keyValues.Length));
            }

            for (var i = 0; i < keyValues.Length; i++)
            {
                if (keyValues[i] == null)
                {
                    throw new ArgumentNullException(nameof(keyValues));
                }

                var valueType = keyValues[i].GetType();
                var propertyType = keyProperties[i].ClrType;
                if (valueType != propertyType)
                {
                    throw new ArgumentException(
                        CoreStrings.FindValueTypeMismatch(
                            i, typeof(TEntity).ShortDisplayName(), valueType.ShortDisplayName(),
                            propertyType.ShortDisplayName()));
                }
            }

            IQueryable<TEntity> query = context.Set<TEntity>().AsNoTracking();
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            for (var i = 0; i < keyProperties.Count; i++)
            {
                var property = keyProperties[i];
                var keyValue = keyValues[i];
                var expression = Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(parameter, property.Name),
                        Expression.Constant(keyValue)),
                    parameter) as Expression<Func<TEntity, bool>>;

                query = query.Where(expression);
            }
            return query;
        }
    }
}