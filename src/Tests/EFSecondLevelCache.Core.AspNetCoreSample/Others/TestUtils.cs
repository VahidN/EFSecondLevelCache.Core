using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;

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
    }
}