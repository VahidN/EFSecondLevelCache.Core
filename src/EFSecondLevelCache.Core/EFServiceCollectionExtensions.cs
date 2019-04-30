using EFSecondLevelCache.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// ServiceCollection Extensions
    /// </summary>
    public static class EFServiceCollectionExtensions
    {
        /// <summary>
        /// A collection of service descriptors.
        /// </summary>
        public static IServiceCollection ServiceCollection { get; set; }

        /// <summary>
        /// Registers the required services of the EFSecondLevelCache.Core.
        /// </summary>
        public static IServiceCollection AddEFSecondLevelCache(this IServiceCollection services)
        {
            services.AddSingleton<IEFCacheKeyHashProvider, EFCacheKeyHashProvider>();
            services.AddSingleton<IEFCacheKeyProvider, EFCacheKeyProvider>();
            services.AddSingleton<IEFCacheServiceProvider, EFCacheServiceProvider>();

            ServiceCollection = services;
            return services;
        }
    }
}