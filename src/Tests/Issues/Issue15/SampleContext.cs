using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Core;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Issue15
{
    public class SampleContext : DbContext
    {
        private static readonly IEFCacheServiceProvider _efCacheServiceProvider =
                ConfigureServices.GetEFCacheServiceProvider();

        public DbSet<Payment> Payments { get; set; }

        public SampleContext()
        { }

        public SampleContext(DbContextOptions options)
            : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=EFSecondLevelCache.Issue15;AttachDbFilename=|DataDirectory|\EFSecondLevelCache.Issue15.mdf;Integrated Security=True;MultipleActiveResultSets=True;"
                .Replace("|DataDirectory|", Path.Combine(Directory.GetCurrentDirectory(), "app_data")));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
                       {
                           entity.HasIndex(e => new { e.IdPaymentType, e.Language }).IsUnique();
                       });
        }

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
    }
}
