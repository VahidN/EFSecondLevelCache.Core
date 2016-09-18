using System.Collections.Generic;

namespace EFSecondLevelCache.Core.Contracts
{
    /// <summary>
    /// Expression and its Dependencies
    /// </summary>
    public class EFQueryDebugView
    {
        /// <summary>
        ///  Dependency items.
        /// </summary>
        public ISet<string> Types { set; get; }

        /// <summary>
        /// Expression to a readable string.
        /// </summary>
        public string DebugView { set; get; }
    }
}