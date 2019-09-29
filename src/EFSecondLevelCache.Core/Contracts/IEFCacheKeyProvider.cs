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
        /// <param name="query">The EF query.</param>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <returns>Information of the computed key of the input LINQ query.</returns>
        EFCacheKey GetEFCacheKey(IQueryable query, Expression expression, string saltKey = "");
    }
}