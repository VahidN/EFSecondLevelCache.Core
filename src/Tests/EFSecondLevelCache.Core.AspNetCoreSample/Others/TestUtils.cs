using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EFSecondLevelCache.Core.AspNetCoreSample.Others
{
    public static class TestUtils
    {
        public static IEnumerable<T> DynamicGetWithCacheableAtFirst<T>(
            this IQueryable<T> query,
            EFCacheDebugInfo debugInfo,
            Expression<Func<T, bool>> filter = null,
            params Expression<Func<T, object>>[] include) where T : class
        {
            query = query.Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(5), debugInfo);

            if (filter != null)
                query = query.Where(filter);

            if (include != null)
            {
                foreach (var includeProperty in include.ToList())
                    query = query.Include(includeProperty);
            }
            return query.ToList();
        }

        public static IEnumerable<T> DynamicGetWithCacheableAtEnd<T>(
            this IQueryable<T> query,
            EFCacheDebugInfo debugInfo,
            Expression<Func<T, bool>> filter = null,
            params Expression<Func<T, object>>[] include) where T : class
        {
            if (filter != null)
                query = query.Where(filter);

            if (include != null)
            {
                foreach (var includeProperty in include.ToList())
                    query = query.Include(includeProperty);
            }
            query = query.Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(5), debugInfo);
            return query.ToList();
        }

        public static async Task<TEntity> GetFirstOrDefaultAsync<TEntity, TDbContext>(
               this TDbContext dbContext,
               EFCacheDebugInfo eFCacheDebugInfo,
               Expression<Func<TEntity, bool>> predicate = null,
               Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
               Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
               bool disableTracking = true,
               CancellationToken cancellationToken = default(CancellationToken))
            where TEntity : class
            where TDbContext : DbContext
        {
            IQueryable<TEntity> query = dbContext.Set<TEntity>();
            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                return await orderBy(query)
                        .Cacheable(eFCacheDebugInfo)
                        .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                return await query
                        .Cacheable(eFCacheDebugInfo)
                        .FirstOrDefaultAsync(cancellationToken);
            }
        }
    }
}