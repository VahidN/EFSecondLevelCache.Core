using System.Collections.Generic;

namespace EFSecondLevelCache.Core.Contracts
{
    /// <summary>
    /// Stores information of the computed key of the input LINQ query.
    /// </summary>
    public class EFCacheKey
    {
        /// <summary>
        /// The computed key of the input LINQ query.
        /// </summary>
        public string Key { set; get; }

        /// <summary>
        /// Hash of the input LINQ query's computed key.
        /// </summary>
        public string KeyHash { set; get; }

        /// <summary>
        /// Determines which entities are used in this LINQ query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </summary>
        public ISet<string> CacheDependencies { set; get; }

        /// <summary>
        /// Stores information of the computed key of the input LINQ query.
        /// </summary>
        public EFCacheKey()
        {
            CacheDependencies = new HashSet<string>();
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var efCacheKey = obj as EFCacheKey;
            if (efCacheKey == null)
                return false;

            return this.KeyHash == efCacheKey.KeyHash;
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + KeyHash.GetHashCode();
                return hash;
            }
        }
    }
}