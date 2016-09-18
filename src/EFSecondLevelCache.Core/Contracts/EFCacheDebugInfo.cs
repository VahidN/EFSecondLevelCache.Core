namespace EFSecondLevelCache.Core.Contracts
{
    /// <summary>
    /// Stores the debug information of the caching process.
    /// </summary>
    public class EFCacheDebugInfo
    {
        /// <summary>
        /// Stores information of the computed key of the input LINQ query.
        /// </summary>
        public EFCacheKey EFCacheKey { set; get; }

        /// <summary>
        /// Determines this query is using the 2nd level cache or not.
        /// </summary>
        public bool IsCacheHit { set; get; }
    }
}