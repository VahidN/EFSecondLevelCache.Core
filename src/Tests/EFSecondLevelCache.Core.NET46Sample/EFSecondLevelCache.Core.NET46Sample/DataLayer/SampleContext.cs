using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Core.Contracts;
using EFSecondLevelCache.Core.NET46Sample.DataLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFSecondLevelCache.Core.NET46Sample.DataLayer
{
    public class SampleContext : DbContext
    {
        private static readonly IEFCacheServiceProvider _efCacheServiceProvider = ConfigureServices.GetEFCacheServiceProvider();

        public virtual DbSet<Post> Posts { get; set; }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled = false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = base.SaveChanges();
            this.ChangeTracker.AutoDetectChangesEnabled = true;


            _efCacheServiceProvider.InvalidateCacheDependencies(changedEntityNames);

            return result;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled = false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = base.SaveChangesAsync(cancellationToken);
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            _efCacheServiceProvider.InvalidateCacheDependencies(changedEntityNames);

            return result;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase();
        }
    }
}
