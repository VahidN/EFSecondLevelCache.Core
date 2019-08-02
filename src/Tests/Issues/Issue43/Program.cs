using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Core;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using CacheManager.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Issue43
{
    public static class ConfigureServices
    {
        private static readonly Lazy<IServiceProvider> _serviceProviderBuilder =
            new Lazy<IServiceProvider>(getServiceProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// A lazy loaded thread-safe singleton
        /// </summary>
        public static IServiceProvider Instance { get; } = _serviceProviderBuilder.Value;

        public static IEFCacheServiceProvider GetEFCacheServiceProvider()
        {
            return Instance.GetRequiredService<IEFCacheServiceProvider>();
        }

        private static IServiceProvider getServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddEFSecondLevelCache();

            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                    .WithJsonSerializer()
                    .WithMicrosoftMemoryCacheHandle(instanceName: "MemoryCache1")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                    .DisablePerformanceCounters()
                    .DisableStatistics()
                    .Build());

            return services.BuildServiceProvider();
        }
    }

    public class User
    {
        public int Id { set; get; }
        public string Name { set; get; }
    }

    public class SampleContext : DbContext
    {
        private static readonly IEFCacheServiceProvider _efCacheServiceProvider =
            ConfigureServices.GetEFCacheServiceProvider();

        public DbSet<User> Users { get; set; }

        public SampleContext()
        {
        }

        public SampleContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=EFSecondLevelCache.Issue43;AttachDbFilename=|DataDirectory|\EFSecondLevelCache.Issue43.mdf;Integrated Security=True;MultipleActiveResultSets=True;"
                    .Replace("|DataDirectory|", Path.Combine(Directory.GetCurrentDirectory(), "app_data"));
            optionsBuilder.UseSqlServer(connectionString);
        }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled =
                false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = base.SaveChanges();
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            _efCacheServiceProvider.InvalidateCacheDependencies(changedEntityNames);

            return result;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled =
                false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = base.SaveChangesAsync(cancellationToken);
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            _efCacheServiceProvider.InvalidateCacheDependencies(changedEntityNames);

            return result;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            SetupDatabase();

            var serviceProvider = ConfigureServices.Instance;

            using (var context = new SampleContext())
            {
                var debugInfo = new EFCacheDebugInfo();
                var item = await context.Users.Where(x => x.Name == "user-1").Cacheable(debugInfo, serviceProvider).FirstOrDefaultAsync();
                Console.WriteLine($"FirstOrDefaultAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = await context.Users.Where(x => x.Name == "user-1").Cacheable(debugInfo, serviceProvider).FirstOrDefaultAsync();
                Console.WriteLine($"FirstOrDefaultAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = await context.Users.Where(x => x.Name == "user-11").Cacheable(debugInfo, serviceProvider).FirstOrDefaultAsync();
                Console.WriteLine($"FirstOrDefaultAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item?.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = await context.Users.Where(x => x.Name == "user-11").Cacheable(debugInfo, serviceProvider).FirstOrDefaultAsync();
                Console.WriteLine($"FirstOrDefaultAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item?.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = context.Users.Cacheable(debugInfo, serviceProvider).Find(1);
                Console.WriteLine($"Find->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item?.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = context.Users.Cacheable(debugInfo, serviceProvider).Find(1);
                Console.WriteLine($"Find->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item?.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = await context.Users.Cacheable(debugInfo, serviceProvider).FindAsync(1);
                Console.WriteLine($"FindAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item?.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = await context.Users.Cacheable(debugInfo, serviceProvider).FindAsync(1);
                Console.WriteLine($"FindAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item?.Name}");

                debugInfo = new EFCacheDebugInfo();
                var items = await context.Users.Where(x => x.Name == "user-1").Cacheable(debugInfo, serviceProvider).ToListAsync();
                Console.WriteLine($"ToListAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{items[0].Name}");

                debugInfo = new EFCacheDebugInfo();
                items = await context.Users.Where(x => x.Name == "user-1").Cacheable(debugInfo, serviceProvider).ToListAsync();
                Console.WriteLine($"ToListAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{items[0].Name}");

                debugInfo = new EFCacheDebugInfo();
                items = context.Users.Where(x => x.Name == "user-2").Cacheable(debugInfo, serviceProvider).ToList();
                Console.WriteLine($"ToList->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{items[0].Name}");

                debugInfo = new EFCacheDebugInfo();
                items = context.Users.Where(x => x.Name == "user-2").Cacheable(debugInfo, serviceProvider).ToList();
                Console.WriteLine($"ToList->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{items[0].Name}");

                debugInfo = new EFCacheDebugInfo();
                item = context.Users.Where(x => x.Name == "user-3").Cacheable(debugInfo, serviceProvider).FirstOrDefault();
                Console.WriteLine($"FirstOrDefault->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = context.Users.Where(x => x.Name == "user-3").Cacheable(debugInfo, serviceProvider).FirstOrDefault();
                Console.WriteLine($"FirstOrDefault->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item.Name}");

                debugInfo = new EFCacheDebugInfo();
                var count = context.Users.Where(x => x.Name == "user-3").Cacheable(debugInfo, serviceProvider).Count();
                Console.WriteLine($"Count->IsCacheHit: {debugInfo.IsCacheHit}, count:{count}");

                debugInfo = new EFCacheDebugInfo();
                count = context.Users.Where(x => x.Name == "user-3").Cacheable(debugInfo, serviceProvider).Count();
                Console.WriteLine($"Count->IsCacheHit: {debugInfo.IsCacheHit}, count:{count}");

                debugInfo = new EFCacheDebugInfo();
                item = await context.Users.Where(x => x.Name == "user-1").Cacheable(debugInfo, serviceProvider).SingleOrDefaultAsync();
                Console.WriteLine($"SingleOrDefaultAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item.Name}");

                debugInfo = new EFCacheDebugInfo();
                item = await context.Users.Where(x => x.Name == "user-1").Cacheable(debugInfo, serviceProvider).SingleOrDefaultAsync();
                Console.WriteLine($"SingleOrDefaultAsync->IsCacheHit: {debugInfo.IsCacheHit}, items[0]:{item.Name}");
            }
        }

        private static void SetupDatabase()
        {
            using (var db = new SampleContext())
            {
                if (db.Database.EnsureCreated())
                {
                    var item1 = new User { Name = "user-1" };
                    db.Users.Add(item1);

                    var item2 = new User { Name = "user-2" };
                    db.Users.Add(item2);

                    var item3 = new User { Name = "user-3" };
                    db.Users.Add(item3);

                    db.SaveChanges();
                }
            }
        }
    }
}