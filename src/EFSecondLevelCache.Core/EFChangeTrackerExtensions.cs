using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Change Tracker Extensions
    /// </summary>
    public static class EFChangeTrackerExtensions
    {
        private static readonly MethodInfo _asNoTrackingMethodInfo =
            typeof(EntityFrameworkQueryableExtensions).GetTypeInfo().GetDeclaredMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking));

        /// <summary>
        /// Find the base types of the given type, recursively.
        /// </summary>
        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            if (type.GetTypeInfo().BaseType == null)
            {
                return type.GetInterfaces();
            }

            return Enumerable.Repeat(type.GetTypeInfo().BaseType, 1)
                             .Concat(type.GetInterfaces())
                             .Concat(type.GetInterfaces().SelectMany(GetBaseTypes))
                             .Concat(type.GetTypeInfo().BaseType.GetBaseTypes());
        }

        /// <summary>
        /// Using the ChangeTracker to find names of the changed entities.
        /// </summary>
        public static string[] GetChangedEntityNames(this DbContext dbContext)
        {
            var typesList = new List<Type>();
            foreach (var type in dbContext.GetChangedEntityTypes())
            {
                typesList.Add(type);
                typesList.AddRange(type.GetBaseTypes().Where(t => t != typeof(object)).ToList());
            }

            var changedEntityNames = typesList
                .Select(type => type.FullName)
                .Distinct()
                .ToArray();

            return changedEntityNames;
        }

        /// <summary>
        /// Using the ChangeTracker to find types of the changed entities.
        /// </summary>
        public static IEnumerable<Type> GetChangedEntityTypes(this DbContext dbContext)
        {
            return dbContext.ChangeTracker.Entries().Where(
                            dbEntityEntry => dbEntityEntry.State == EntityState.Added ||
                            dbEntityEntry.State == EntityState.Modified ||
                            dbEntityEntry.State == EntityState.Deleted)
                .Select(dbEntityEntry => dbEntityEntry.Entity.GetType());
        }

        /// <summary>
        /// Applies the AsNoTracking method dynamically
        /// </summary>
        public static IQueryable<TType> MarkAsNoTracking<TType>(this IQueryable<TType> query)
        {
            if (typeof(TType).GetTypeInfo().IsClass)
            {
                return query.Provider.CreateQuery<TType>(
                    Expression.Call(null, _asNoTrackingMethodInfo.MakeGenericMethod(typeof(TType)), query.Expression));
            }
            return query;
        }
    }
}