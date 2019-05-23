using System;
using System.IO;
using CacheManager.Core;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Newtonsoft.Json;

namespace EFSecondLevelCache.Core.PerformanceTests
{
    /// <summary>
    /// A lazy loaded thread-safe singleton
    /// </summary>
    public static class TestsServiceProvider
    {
        private static readonly Lazy<IServiceProvider> _jsonSerializerProviderBuilder =
            new Lazy<IServiceProvider>(getWithJsonSerializerServiceProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<IServiceProvider> _gzJsonSerializerProviderBuilder =
            new Lazy<IServiceProvider>(getWithGzJsonSerializerServiceProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<IServiceProvider> _dictionaryHandleProviderBuilder =
            new Lazy<IServiceProvider>(getWithDictionaryHandleServiceProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<IServiceProvider> _microsoftMemoryCacheProviderBuilder =
            new Lazy<IServiceProvider>(getWithMicrosoftMemoryCacheHandleServiceProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        public static IServiceProvider WithJsonSerializerInstance { get; } = _jsonSerializerProviderBuilder.Value;

        public static IServiceProvider WithGzJsonSerializerInstance { get; } = _gzJsonSerializerProviderBuilder.Value;

        public static IServiceProvider WithDictionaryHandleInstance { get; } = _dictionaryHandleProviderBuilder.Value;

        public static IServiceProvider WithMicrosoftMemoryCacheInstance { get; } = _microsoftMemoryCacheProviderBuilder.Value;

        private static IServiceProvider getWithJsonSerializerServiceProvider()
        {
            return createServiceProvider(sc => addJsonSerializer(sc));
        }

        private static IServiceProvider getWithGzJsonSerializerServiceProvider()
        {
            return createServiceProvider(sc => addGzJsonSerializer(sc));
        }

        private static IServiceProvider getWithDictionaryHandleServiceProvider()
        {
            return createServiceProvider(sc => addDictionaryHandle(sc));
        }

        private static IServiceProvider getWithMicrosoftMemoryCacheHandleServiceProvider()
        {
            return createServiceProvider(sc => addMicrosoftMemoryCacheHandle(sc));
        }

        private static void addJsonSerializer(ServiceCollection services)
        {
            var jss = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                        .WithJsonSerializer(serializationSettings: jss, deserializationSettings: jss)
                        .WithUpdateMode(CacheUpdateMode.Up)
                        .WithMicrosoftMemoryCacheHandle(instanceName: "MemoryCache1")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .DisablePerformanceCounters()
                        .DisableStatistics()
                        .Build());
        }

        private static void addGzJsonSerializer(ServiceCollection services)
        {
            var jss = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                        .WithGzJsonSerializer(serializationSettings: jss, deserializationSettings: jss)
                        .WithUpdateMode(CacheUpdateMode.Up)
                        .WithMicrosoftMemoryCacheHandle(instanceName: "MemoryCache2")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .DisablePerformanceCounters()
                        .DisableStatistics()
                        .Build());
        }

        private static void addDictionaryHandle(ServiceCollection services)
        {
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                        .WithDictionaryHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .DisablePerformanceCounters()
                        .DisableStatistics()
                        .Build());
        }

        private static void addMicrosoftMemoryCacheHandle(ServiceCollection services)
        {
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                        .WithMicrosoftMemoryCacheHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .DisablePerformanceCounters()
                        .DisableStatistics()
                        .Build());
        }

        private static IServiceProvider createServiceProvider(Action<ServiceCollection> config)
        {
            var services = new ServiceCollection();

            services.AddEFSecondLevelCache();

            config(services);

            var connectionString = getConnectionString();
            services.AddDbContext<SampleContext>(optionsBuilder => optionsBuilder.UseSqlServer(connectionString));

            return services.BuildServiceProvider();
        }

        private static string getConnectionString()
        {
            var appPath = Environment.CurrentDirectory.Split(new[] { "bin" }, StringSplitOptions.None);
            var appDataDir = Path.Combine(appPath[0], "app_data");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            var connectionString = "Server=(localdb)\\mssqllocaldb;Initial Catalog=EFSecondLevelCacheCore.Perf.Test;AttachDBFilename=|DataDirectory|\\EFSecondLevelCacheCore.Perf.Test.mdf;Trusted_Connection=True;"
            .Replace("|DataDirectory|", appDataDir);
            Console.WriteLine($"Using {connectionString}");
            return connectionString;
        }
    }
}