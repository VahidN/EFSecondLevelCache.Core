using System;
using AutoMapper;
using CacheManager.Core;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using EFSecondLevelCache.Core.AspNetCoreSample.Profiles;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace EFSecondLevelCache.Core.AspNetCoreSample
{
    public class Startup
    {
        private readonly string _contentRootPath;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _contentRootPath = env.ContentRootPath;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IServiceScopeFactory scopeFactory)
        {
            //app.UseBlockingDetection();

            scopeFactory.Initialize();
            scopeFactory.SeedData();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEFSecondLevelCache();
            // addInMemoryCacheServiceProvider(services);
            addRedisCacheServiceProvider(services);

            services.AddDbContext<SampleContext>(optionsBuilder =>
            {
                var useInMemoryDatabase = Configuration["UseInMemoryDatabase"].Equals("true", StringComparison.OrdinalIgnoreCase);
                if (useInMemoryDatabase)
                {
                    optionsBuilder.UseInMemoryDatabase("TestDb");
                }
                else
                {
                    var connectionString = Configuration["ConnectionStrings:ApplicationDbContextConnection"];
                    if (connectionString.Contains("%CONTENTROOTPATH%"))
                    {
                        connectionString = connectionString.Replace("%CONTENTROOTPATH%", _contentRootPath);
                    }
                    optionsBuilder.UseSqlServer(
                        connectionString
                        , serverDbContextOptionsBuilder =>
                        {
                            var minutes = (int)TimeSpan.FromMinutes(3).TotalSeconds;
                            serverDbContextOptionsBuilder.CommandTimeout(minutes);
                        });
                    optionsBuilder.EnableSensitiveDataLogging();
                    optionsBuilder.ConfigureWarnings(w =>
                    {
                    });
                }
            });

            services.AddAutoMapper(typeof(PostProfile).GetTypeInfo().Assembly);
            services.AddControllersWithViews();
        }

        private static void addInMemoryCacheServiceProvider(IServiceCollection services)
        {
            var jss = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                    .WithJsonSerializer(serializationSettings: jss, deserializationSettings: jss)
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
                    .Build());
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
        }
    }
}