using System;
using System.Linq;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;

namespace EFSecondLevelCache.Core.Tests
{
    [TestClass]
    public class EFCachedQueryProviderInvalidationTests
    {
        [TestMethod]
        public void TestInsertingDataIntoTheSameTableShouldInvalidateTheCacheAutomatically()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product2";

                    Console.WriteLine("1st query, reading from db");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo1, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("same query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());


                    Console.WriteLine("inserting data, invalidates the cache on SaveChanges");
                    var rnd = new Random();
                    var newProduct = new Product
                    {
                        IsActive = false,
                        ProductName = $"Product{rnd.Next()}",
                        ProductNumber = rnd.Next().ToString(),
                        Notes = "Notes ...",
                        UserId = 1
                    };
                    context.Products.Add(newProduct);
                    context.SaveChanges();


                    Console.WriteLine("same query after insert, reading from database.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                }
            }
        }

        [TestMethod]
        public void TestInsertingDataToOtherTablesShouldNotInvalidateTheCacheDependencyAutomatically()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product3";

                    Console.WriteLine("1st query, reading from db (it dependes on/includes the Tags table)");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo1, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());


                    Console.WriteLine(
                        "inserting data into a *non-related* table, shouldn't invalidate the cache on SaveChanges.");
                    var rnd = new Random();
                    var user = new User
                    {
                        Name = $"User {rnd.Next()}"
                    };
                    context.Users.Add(user);
                    context.SaveChanges();


                    Console.WriteLine("same query after insert, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                }
            }
        }

        [TestMethod]
        public void TestInsertingDataToRelatedTablesShouldInvalidateTheCacheDependencyAutomatically()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product1";

                    Console.WriteLine("1st query, reading from db (it dependes on/includes the Tags table).");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo1, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("same query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());


                    Console.WriteLine("inserting data into a *related* table, invalidates the cache on SaveChanges.");
                    var rnd = new Random();
                    var tag = new Tag
                    {
                        Name = $"Tag {rnd.Next()}"
                    };
                    context.Tags.Add(tag);
                    context.SaveChanges();


                    Console.WriteLine(
                        "same query after insert, reading from database (it dependes on/includes the Tags table)");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                }
            }
        }

        [TestMethod]
        public void TestTransactionRollbackShouldNotInvalidateTheCacheDependencyAutomatically()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product1";

                    Console.WriteLine("1st query, reading from db.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo1, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());

                    Console.WriteLine(
                        "inserting data with transaction.Rollback, shouldn't invalidate the cache on SaveChanges.");
                    try
                    {
                        var rnd = new Random();
                        var newProduct = new Product
                        {
                            IsActive = false,
                            ProductName = "Product1", // It has an `IsUnique` constraint.
                            ProductNumber = rnd.Next().ToString(),
                            Notes = "Notes ...",
                            UserId = 1
                        };
                        context.Products.Add(newProduct);
                        context.SaveChanges(); // it uses a transaction behind the scene.
                    }
                    catch (Exception ex)
                    {
                        // NOTE: This doesn't work with `EntityFrameworkInMemoryDatabase`. Because it doesn't support constraints.
                        // ProductName is duplicate here and should throw an exception on save changes
                        // and rollback the transaction automatically.
                        Console.WriteLine(ex.ToString());
                    }

                    Console.WriteLine("same query after insert, reading from 2nd level cache.");

                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                }
            }
        }

        [TestMethod]
        public void TestRemoveDataShouldInvalidateTheCacheAutomatically()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = false;
                    var name = "Product4";

                    Console.WriteLine("1st query, reading from db");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo1, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsNotNull(list1);


                    Console.WriteLine("same query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());


                    Console.WriteLine("removing data, invalidates the cache on SaveChanges");
                    var product1 = context.Products.First(product => product.ProductName == name);
                    context.Products.Remove(product1);
                    context.SaveChanges();


                    Console.WriteLine("same query after remove, reading from database.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.IsNotNull(list3);
                }
            }
        }

        [TestMethod]
        public void TestRemoveTptDataShouldInvalidateTheCacheAutomatically()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query, reading from db");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Posts.OfType<Page>().Cacheable(debugInfo1, serviceProvider).ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.AreEqual(2, list1.Count);


                    Console.WriteLine("same query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Posts.OfType<Page>().Cacheable(debugInfo2, serviceProvider).ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.AreEqual(2, list2.Count);


                    Console.WriteLine("removing data, invalidates the cache on SaveChanges");
                    var post1 = context.Posts.First(post => post.Title == "Post1");
                    context.Posts.Remove(post1);
                    context.SaveChanges();


                    Console.WriteLine("same query after remove, reading from database.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Posts.OfType<Page>().Cacheable(debugInfo3, serviceProvider).ToList();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.AreEqual(1, list3.Count);
                }
            }
        }
    }
}