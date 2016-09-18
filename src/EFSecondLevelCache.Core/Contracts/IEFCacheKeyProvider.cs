using System.Linq;
using System.Linq.Expressions;

namespace EFSecondLevelCache.Core.Contracts
{
    /// <summary>
    /// CacheKeyProvider Contract.
    /// </summary>
    public interface IEFCacheKeyProvider
    {
        /// <summary>
        /// Gets an EF query and returns its hash to store in the cache.
        /// </summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <param name="query">The EF query.</param>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <returns>Information of the computed key of the input LINQ query.</returns>
        EFCacheKey GetEFCacheKey<T>(IQueryable<T> query, Expression expression, string saltKey = "");
    }
}