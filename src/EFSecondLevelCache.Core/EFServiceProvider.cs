using System;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Application's IServiceProvider.
    /// </summary>
    public static class EFServiceProvider
    {
        /// <summary>
        /// Access point of the application's IServiceProvider.
        /// </summary>
        public static IServiceProvider ApplicationServices { get; set; }
    }
}