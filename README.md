EFSecondLevelCache.Core
=======
[![Build status](https://ci.appveyor.com/api/projects/status/2vulcij72pc59ghv?svg=true)](https://ci.appveyor.com/project/VahidN/efsecondlevelcache-core)

Entity Framework Core Second Level Caching Library.

Second level caching is a query cache. The results of EF commands will be stored in the cache, so that the same EF commands will retrieve their data from the cache rather than executing them against the database again.

Install via NuGet
-----------------
To install EFSecondLevelCache.Core, run the following command in the Package Manager Console:

```
PM> Install-Package EFSecondLevelCache.Core
```

You can also view the [package page](http://www.nuget.org/packages/EFSecondLevelCache.Core/) on NuGet.

This library also uses the [CacheManager.Core](https://github.com/MichaCo/CacheManager), as a highly configurable cache manager.
To use its in-memory caching mechanism, add these entries to the `.csproj` file:

```xml
  <ItemGroup>
    <PackageReference Include="EFSecondLevelCache.Core" Version="1.6.2" />
    <PackageReference Include="CacheManager.Core" Version="1.1.2" />
    <PackageReference Include="CacheManager.Microsoft.Extensions.Caching.Memory" Version="1.1.2" />
    <PackageReference Include="CacheManager.Serialization.Json" Version="1.1.2" />
  </ItemGroup>
```

And to get the latest versions of these libraries you can run the following command in the Package Manager Console:
```
PM> Update-Package
```


Usage
-----
1- [Register the required services](/src/Tests/EFSecondLevelCache.Core.AspNetCoreSample/Startup.cs) of `EFSecondLevelCache.Core` and also `CacheManager.Core`
```csharp
namespace EFSecondLevelCache.Core.AspNetCoreSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEFSecondLevelCache();

            // Add an in-memory cache service provider
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                        .WithJsonSerializer()
                        .WithMicrosoftMemoryCacheHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .Build());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseEFSecondLevelCache();
        }
    }
}
```

If you want to use the Redis as the preferred cache provider, first install the `CacheManager.StackExchange.Redis` package and then register its required services:
```csharp
// Add Redis cache service provider
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
```


2- [Setting up the cache invalidation](/src/Tests/EFSecondLevelCache.Core.AspNetCoreSample/DataLayer/SampleContext.cs) by overriding the SaveChanges method to prevent stale reads:

```csharp
namespace EFSecondLevelCache.Core.AspNetCoreSample.DataLayer
{
    public class SampleContext : DbContext
    {
        public SampleContext(DbContextOptions<SampleContext> options) : base(options)
        { }

        public virtual DbSet<Post> Posts { get; set; }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            var result = base.SaveChanges();
            this.GetService<IEFCacheServiceProvider>().InvalidateCacheDependencies(changedEntityNames);

            return result;
        }
    }
}
```



3- Then to cache the results of the normal queries like:
```csharp
var products = context.Products.Include(x => x.Tags).FirstOrDefault();
```
We can use the new `Cacheable()` extension method:
```csharp
var products = context.Products.Include(x => x.Tags).Cacheable().FirstOrDefault(); // Async methods are supported too.
```


Guidance
--------

### When to use
Good candidates for query caching are global site settings and public data, such as infrequently changing articles or comments. It can also be beneficial to cache data specific to a user so long as the cache expires frequently enough relative to the size of the user base that memory consuption remains acceptable. Small, per-user data that frequently exceeds the cache's lifetime, such as a user's photo path, is better held in user claims, which are stored in cookies, than in this cache.

### Scope
This cache is scoped to the application, not the current user. It does not use session variables. Accordingly, when retriveing cached per-user data, be sure queries in include code such as `.Where(x => .... && x.UserId == id)`.

### Invalidation
This cache is updated when an entity is changed (insert, update, or delete) via a DbContext that uses this library. If the database is updated through some other means, such as a stored procedure or trigger, the cache becomes stale.
