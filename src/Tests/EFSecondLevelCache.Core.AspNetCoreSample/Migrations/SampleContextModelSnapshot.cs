using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;

namespace EFSecondLevelCache.Core.AspNetCoreSample.Migrations
{
    [DbContext(typeof(SampleContext))]
    partial class SampleContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Post", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Title");

                    b.Property<int>("UserId");

                    b.Property<string>("post_type")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Posts");

                    b.HasDiscriminator<string>("post_type").HasValue("post_base");
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Product", b =>
                {
                    b.Property<int>("ProductId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsActive");

                    b.Property<string>("Notes");

                    b.Property<string>("ProductName")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 50);

                    b.Property<string>("ProductNumber")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 30);

                    b.Property<int>("UserId");

                    b.HasKey("ProductId");

                    b.HasIndex("ProductName")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.TagProduct", b =>
                {
                    b.Property<int>("TagId");

                    b.Property<int>("ProductProductId");

                    b.HasKey("TagId", "ProductProductId");

                    b.HasIndex("ProductProductId");

                    b.HasIndex("TagId");

                    b.ToTable("TagProducts");
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Page", b =>
                {
                    b.HasBaseType("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Post");


                    b.ToTable("Page");

                    b.HasDiscriminator().HasValue("post_page");
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Post", b =>
                {
                    b.HasOne("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.User", "User")
                        .WithMany("Posts")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Product", b =>
                {
                    b.HasOne("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.User", "User")
                        .WithMany("Products")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.TagProduct", b =>
                {
                    b.HasOne("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Product", "Product")
                        .WithMany("TagProducts")
                        .HasForeignKey("ProductProductId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Tag", "Tag")
                        .WithMany("TagProducts")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
