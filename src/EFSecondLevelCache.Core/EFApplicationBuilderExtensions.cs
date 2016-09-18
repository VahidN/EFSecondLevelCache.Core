using Microsoft.AspNetCore.Builder;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Application Builder Extensions
    /// </summary>
    public static class EFApplicationBuilderExtensions
    {
        /// <summary>
        /// Enables EFSecondLevelCache.Core to access ApplicationServices.
        /// </summary>
        public static IApplicationBuilder UseEFSecondLevelCache(this IApplicationBuilder app)
        {
            EFServiceProvider.ApplicationServices = app.ApplicationServices;
            return app;
        }
    }
}