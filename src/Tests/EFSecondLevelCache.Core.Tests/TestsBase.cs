using System;
using System.Collections.Generic;
using CacheManager.Core;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Utils;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EFSecondLevelCache.Core.Tests
{
    public static class TestsBase
    {
        public static IServiceProvider GetServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IConfigurationRoot>(provider =>
            {
                return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                                .AddInMemoryCollection(new[]
                                {
                                    new KeyValuePair<string,string>("UseInMemoryDatabase", "true"),
                                })
                                .Build();
            });

            services.AddEntityFrameworkInMemoryDatabase().AddDbContext<SampleContext>(ServiceLifetime.Scoped);

            services.AddEFSecondLevelCache();

            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                        .WithJsonSerializer()
                        .WithMicrosoftMemoryCacheHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .DisablePerformanceCounters()
                        .DisableStatistics()
                        .Build());

            var serviceProvider = services.BuildServiceProvider();
            EFServiceProvider.ApplicationServices = serviceProvider; // app.UseEFSecondLevelCache();

            var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            serviceScope.SeedData();

            return serviceProvider;
        }

        public static IEFCacheServiceProvider GetCacheServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddEFSecondLevelCache();

            services.AddSingleton(typeof(ICacheManagerConfiguration),
               new CacheManager.Core.ConfigurationBuilder()
                       .WithJsonSerializer()
                       .WithMicrosoftMemoryCacheHandle()
                       .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                       .DisablePerformanceCounters()
                       .DisableStatistics()
                       .Build());
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetService<IEFCacheServiceProvider>();
        }
    }
}