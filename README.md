EFSecondLevelCache.Core
=======
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
To use its in-memory caching mechanism, add these entries to the `project.json` file:

```json
{
    "dependencies": {
        "EFSecondLevelCache.Core": "1.0.1-*",
        "CacheManager.Core": "0.9.1",
        "CacheManager.Microsoft.Extensions.Caching.Memory": "0.9.1",
        "CacheManager.Serialization.Json": "0.9.1"
    }
}
```

And to get the latest versions of these libraries you can run the following command in the Package Manager Console:
```
PM> Update-Package
```


Usage:
------
1- [Register the required services](/src/Tests/EFSecondLevelCache.Core.AspNetCoreSample/Startup.cs) of `EFSecondLevelCache.Core` and also `CacheManager.Core`
```csharp
namespace EFSecondLevelCache.Core.AspNetCoreSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEFSecondLevelCache();

            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(CacheManagerConfiguration),
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

2- [Setting up the cache invalidation](/src/Tests/EFSecondLevelCache.Core.AspNetCoreSample/DataLayer/SampleContext.cs) by overriding the SaveChanges method to prevent stale reads:

```csharp
namespace EFSecondLevelCache.Core.AspNetCoreSample.DataLayer
{
    public class SampleContext : DbContext
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IEFCacheServiceProvider _cacheServiceProvider;

        public SampleContext(IConfigurationRoot configuration, IEFCacheServiceProvider cacheServiceProvider)
        {
            _configuration = configuration;
            _cacheServiceProvider = cacheServiceProvider;
        }

        public virtual DbSet<Post> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            var result = base.SaveChanges();
            _cacheServiceProvider.InvalidateCacheDependencies(changedEntityNames);

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


Notes:
------
Good candidates for query caching are global site's settings,
list of `public` articles or comments and not frequently changed,
private or specific data to each user.
If a page requires authentication, its data shouldn't be cached.
