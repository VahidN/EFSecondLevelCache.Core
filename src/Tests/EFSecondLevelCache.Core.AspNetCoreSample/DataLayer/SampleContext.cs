using System;
using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<TagProduct> TagProducts { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var useInMemoryDatabase = _configuration["UseInMemoryDatabase"].Equals("true", StringComparison.OrdinalIgnoreCase);
            if (useInMemoryDatabase)
            {
                optionsBuilder.UseInMemoryDatabase();
            }
            else
            {
                optionsBuilder.UseSqlServer(
                    _configuration["ConnectionStrings:ApplicationDbContextConnection"]
                    , serverDbContextOptionsBuilder =>
                    {
                        var minutes = (int)TimeSpan.FromMinutes(3).TotalSeconds;
                        serverDbContextOptionsBuilder.CommandTimeout(minutes);
                    });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Posts)
                    .HasForeignKey(d => d.UserId);

                entity.HasDiscriminator<string>("post_type")
                      .HasValue<Post>("post_base")
                      .HasValue<Page>("post_page");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);

                entity.HasIndex(e => e.ProductName)
                    .IsUnique();

                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProductNumber)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<TagProduct>(entity =>
            {
                entity.HasKey(e => new { e.TagId, e.ProductProductId });

                entity.HasIndex(e => e.ProductProductId);

                entity.HasIndex(e => e.TagId);

                entity.Property(e => e.TagId);

                entity.Property(e => e.ProductProductId);

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.TagProducts)
                    .HasForeignKey(d => d.ProductProductId);

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.TagProducts)
                    .HasForeignKey(d => d.TagId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Name).IsRequired();
            });
        }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled = false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = base.SaveChanges();
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            _cacheServiceProvider.InvalidateCacheDependencies(changedEntityNames);

            return result;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            this.ChangeTracker.DetectChanges();
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled = false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = base.SaveChangesAsync(cancellationToken);
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            _cacheServiceProvider.InvalidateCacheDependencies(changedEntityNames);

            return result;
        }
    }
}