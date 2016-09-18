using System.Linq;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Utils
{
    public static class ApplicationDbContextSeedData
    {
        public static void SeedData(this IServiceScopeFactory scopeFactory)
        {
            using (var serviceScope = scopeFactory.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<SampleContext>();
                User user1;

                const string user1Name = "User1";
                if (!context.Users.Any(user => user.Name == user1Name))
                {
                    user1 = new User { Name = user1Name };
                    user1 = context.Users.Add(user1).Entity;
                }
                else
                {
                    user1 = context.Users.First(user => user.Name == user1Name);
                }

                const string product4Name = "Product4";
                if (!context.Products.Any(product => product.ProductName == product4Name))
                {
                    var product4 = new Product
                    {
                        ProductName = product4Name,
                        IsActive = false,
                        Notes = "Notes ...",
                        ProductNumber = "004",
                        User = user1
                    };
                    product4 = context.Products.Add(product4).Entity;

                    var tag4 = new Tag
                    {
                        Name = "Tag4"
                    };
                    context.Tags.Add(tag4);

                    var productTag = new TagProduct { Tag = tag4, Product = product4 };
                    context.TagProducts.Add(productTag);
                }

                const string product1Name = "Product1";
                if (!context.Products.Any(product => product.ProductName == product1Name))
                {
                    var product1 = new Product
                    {
                        ProductName = product1Name,
                        IsActive = true,
                        Notes = "Notes ...",
                        ProductNumber = "001",
                        User = user1
                    };
                    product1 = context.Products.Add(product1).Entity;

                    var tag1 = new Tag
                    {
                        Name = "Tag1"
                    };
                    context.Tags.Add(tag1);

                    var productTag = new TagProduct { Tag = tag1, Product = product1 };
                    context.TagProducts.Add(productTag);
                }


                const string product2Name = "Product2";
                if (!context.Products.Any(product => product.ProductName == product2Name))
                {
                    var product2 = new Product
                    {
                        ProductName = product2Name,
                        IsActive = true,
                        Notes = "Notes ...",
                        ProductNumber = "002",
                        User = user1
                    };
                    product2 = context.Products.Add(product2).Entity;

                    var tag2 = new Tag
                    {
                        Name = "Tag2"
                    };
                    context.Tags.Add(tag2);

                    var productTag = new TagProduct { Tag = tag2, Product = product2 };
                    context.TagProducts.Add(productTag);
                }

                const string product3Name = "Product3";
                if (!context.Products.Any(product => product.ProductName == product3Name))
                {
                    var product3 = new Product
                    {
                        ProductName = product3Name,
                        IsActive = true,
                        Notes = "Notes ...",
                        ProductNumber = "003",
                        User = user1
                    };
                    product3 = context.Products.Add(product3).Entity;

                    var tag3 = new Tag
                    {
                        Name = "Tag3"
                    };
                    context.Tags.Add(tag3);

                    var productTag = new TagProduct { Tag = tag3, Product = product3 };
                    context.TagProducts.Add(productTag);
                }

                const string post1Title = "Post1";
                if (!context.Posts.Any(post => post.Title == post1Title))
                {
                    var page1 = new Page
                    {
                        Title = post1Title,
                        User = user1
                    };
                    context.Posts.Add(page1);
                }

                const string post2Title = "Post2";
                if (!context.Posts.Any(post => post.Title == post2Title))
                {
                    var page2 = new Page
                    {
                        Title = post2Title,
                        User = user1
                    };
                    context.Posts.Add(page2);
                }

                context.SaveChanges();
            }
        }
    }
}