using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using CacheManager.Core;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Utils;
using EFSecondLevelCache.Core.AspNetCoreSample.Profiles;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

//[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)] // Workers: The number of threads to run the tests. Set it to 0 to use the number of core of your computer.
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
            return serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
        }

        public static IEFCacheServiceProvider GetRedisCacheServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddEFSecondLevelCache();

            addRedisCacheServiceProvider(services);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IEFCacheServiceProvider>();
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

            services.AddEntityFrameworkInMemoryDatabase().AddDbContext<SampleContext>(optionsBuilder =>
            {
                optionsBuilder.UseInMemoryDatabase("TestDb");
            });

            services.AddAutoMapper(typeof(PostProfile).GetTypeInfo().Assembly);

            services.AddEFSecondLevelCache();

            addInMemoryCacheServiceProvider(services);
            //addRedisCacheServiceProvider(services);

            var serviceProvider = services.BuildServiceProvider();
            var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            serviceScope.SeedData();

            return serviceProvider;
        }

        public static void ExecuteInParallel(Action test, int count = 40)
        {
            var tests = new Action[count];
            for (var i = 0; i < count; i++)
            {
                tests[i] = test;
            }
            Parallel.Invoke(tests);
        }

        private static void addInMemoryCacheServiceProvider(IServiceCollection services)
        {
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                    .WithJsonSerializer()
                    .WithMicrosoftMemoryCacheHandle(instanceName: "MemoryCache1")
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
                            .WithEndpoint("localhost", 6379)
                            // Enables keyspace notifications to react on eviction/expiration of items.
                            // Make sure that all servers are configured correctly and 'notify-keyspace-events' is at least set to 'Exe', otherwise CacheManager will not retrieve any events.
                            // See https://redis.io/topics/notifications#configuration for configuration details.
                            .EnableKeyspaceEvents();
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