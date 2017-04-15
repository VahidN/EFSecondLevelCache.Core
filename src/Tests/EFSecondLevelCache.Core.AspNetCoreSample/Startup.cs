using System;
using CacheManager.Core;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EFSecondLevelCache.Core;
using Newtonsoft.Json;

namespace EFSecondLevelCache.Core.AspNetCoreSample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                                .SetBasePath(env.ContentRootPath)
                                .AddJsonFile("appsettings.json", reloadOnChange: true, optional: false)
                                .AddJsonFile($"appsettings.{env}.json", optional: true)
                                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { set; get; }
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory)
        {
            scopeFactory.Initialize();
            scopeFactory.SeedData();

            app.UseEFSecondLevelCache();

            loggerFactory.AddDebug(minLevel: LogLevel.Debug);

            if (env.IsDevelopment())
            {
                app.UseDatabaseErrorPage();
                app.UseDeveloperExceptionPage();
            }

            app.UseFileServer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationRoot>(provider => { return Configuration; });
            services.AddDbContext<SampleContext>(ServiceLifetime.Scoped);

            services.AddMvc();
            services.AddDirectoryBrowser();

            services.AddEFSecondLevelCache();
            addInMemoryCacheServiceProvider(services);
            //addRedisCacheServiceProvider(services);
        }

        private static void addInMemoryCacheServiceProvider(IServiceCollection services)
        {
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                    .WithJsonSerializer()
                    .WithMicrosoftMemoryCacheHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                    .Build());
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
                    .Build());
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
        }
    }
}