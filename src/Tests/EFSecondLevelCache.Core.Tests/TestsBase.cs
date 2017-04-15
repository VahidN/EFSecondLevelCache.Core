using System;
using System.Collections.Generic;
using CacheManager.Core;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Utils;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EFSecondLevelCache.Core.Tests
{
    public static class TestsBase
    {
        public static IEFCacheServiceProvider GetInMemoryCacheServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddEFSecondLevelCache();

            addInMemoryCacheServiceProvider(services);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetService<IEFCacheServiceProvider>();
        }

        public static IEFCacheServiceProvider GetRedisCacheServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddEFSecondLevelCache();

            addRedisCacheServiceProvider(services);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetService<IEFCacheServiceProvider>();
        }

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

            addInMemoryCacheServiceProvider(services);
            //addRedisCacheServiceProvider(services);

            var serviceProvider = services.BuildServiceProvider();
            EFServiceProvider.ApplicationServices = serviceProvider; // app.UseEFSecondLevelCache();

            var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            serviceScope.SeedData();

            return serviceProvider;
        }

        private static void addInMemoryCacheServiceProvider(IServiceCollection services)
        {
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                    .WithJsonSerializer()
                    .WithMicrosoftMemoryCacheHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                    .DisablePerformanceCounters()
                    .DisableStatistics()
                    .Build());
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
        }

        private static void addRedisCacheServiceProvider(IServiceCollection services)
        {
            var jss = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            const string redisConfigurationKey = "redis";
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                    .WithJsonSerializer(serializationSettings: jss, deserializationSettings: jss)
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithRedisConfiguration(redisConfigurationKey, config =>
                    {
                        config.WithAllowAdmin()
                            .WithDatabase(0)
                            .WithEndpoint("localhost", 6379);
                    })
                    .WithMaxRetries(100)
                    .WithRetryTimeout(50)
                    .WithRedisCacheHandle(redisConfigurationKey)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                    .DisablePerformanceCounters()
                    .DisableStatistics()
                    .Build());
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
        }
    }
}