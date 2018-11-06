using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.Core.Tests
{
    [TestClass]
    public class EFCachedQueryProviderBasicTests
    {

        [TestMethod]
        public void TestIncludeMethodAffectsKeyCache()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("a normal query");
                    var product1IncludeTags = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag).FirstOrDefault();
                    Assert.IsNotNull(product1IncludeTags);


                    Console.WriteLine("1st query using Include method.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var firstProductIncludeTags = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                                                                 .Cacheable(debugInfo1, serviceProvider)
                                                                 .FirstOrDefault();
                    Assert.IsNotNull(firstProductIncludeTags);
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;
                    var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;

                    Console.WriteLine(
                        @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var firstProduct = context.Products.Cacheable(debugInfo2, serviceProvider)
                                                      .FirstOrDefault();
                    Assert.IsNotNull(firstProduct);
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;
                    var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                    Assert.AreNotEqual(hash1, hash2);
                    Assert.AreNotEqual(cacheDependencies1, cacheDependencies2);
                }
            }
        }

        [TestMethod]
        public void TestQueriesUsingDifferentParameterValuesWillNotUseTheCache()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query, reading from db.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive && product.ProductName == "Product1")
                        .Cacheable(debugInfo1, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsNotNull(list1);

                    Console.WriteLine("2nd query, reading from db.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == false && product.ProductName == "Product1")
                        .Cacheable(debugInfo2, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsNotNull(list2);

                    Console.WriteLine("third query, reading from db.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == false && product.ProductName == "Product2")
                        .Cacheable(debugInfo3, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.IsNotNull(list3);

                    Console.WriteLine("4th query, same as third one, reading from cache.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var list4 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == false && product.ProductName == "Product2")
                        .Cacheable(debugInfo4, serviceProvider)
                        .ToList();
                    Assert.AreEqual(true, debugInfo4.IsCacheHit);
                    Assert.IsNotNull(list4);
                }
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheCreatesTheCommandTreeAfterCallingTheSameNormalQuery()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product3";

                    Console.WriteLine("1st normal query, reading from db.");
                    var list1 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .ToList();
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("same query as Cacheable, reading from db.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;

                    Assert.AreEqual(hash2, hash3);
                }
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheDoesNotHitTheDatabase()
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
                    var list1 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo1, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;

                    Assert.AreEqual(hash1, hash2);
                    Assert.AreEqual(hash2, hash3);

                    Console.WriteLine("different query, reading from db.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var list4 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                       .Cacheable(debugInfo4, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(false, debugInfo4.IsCacheHit);
                    Assert.IsTrue(list4.Any());

                    var hash4 = debugInfo4.EFCacheKey.KeyHash;
                    Assert.AreNotSame(hash3, hash4);
                }
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheInTwoDifferentContextsDoesNotHitTheDatabase()
        {
            var serviceProvider = TestsBase.GetServiceProvider();

            var isActive = true;
            var name = "Product1";
            string hash2;
            string hash3;

            Console.WriteLine("context 1.");
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query as Cacheable, reading from db.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, serviceProvider)
                        .ToList();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());
                    hash2 = debugInfo2.EFCacheKey.KeyHash;
                }
            }

            Console.WriteLine("context 2");
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                    hash3 = debugInfo3.EFCacheKey.KeyHash;
                }
            }

            Assert.AreEqual(hash2, hash3);
        }


        [TestMethod]
        public void TestSecondLevelCacheInTwoDifferentParallelContexts()
        {
            var serviceProvider = TestsBase.GetServiceProvider();

            var isActive = true;
            var name = "Product1";
            var debugInfo2 = new EFCacheDebugInfo();
            var debugInfo3 = new EFCacheDebugInfo();

            var task1 = Task.Factory.StartNew(() =>
            {
                Console.WriteLine("context 1.");
                using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                    {
                        Console.WriteLine("1st query as Cacheable.");
                        var list2 = context.Products
                            .OrderBy(product => product.ProductNumber)
                            .Where(product => product.IsActive == isActive && product.ProductName == name)
                            .Cacheable(debugInfo2, serviceProvider)
                            .ToList();
                        Assert.IsTrue(list2.Any());
                    }
                }
            });

            var task2 = Task.Factory.StartNew(() =>
            {
                Console.WriteLine("context 2");
                using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                    {
                        Console.WriteLine("same query");
                        var list3 = context.Products
                            .OrderBy(product => product.ProductNumber)
                            .Where(product => product.IsActive == isActive && product.ProductName == name)
                            .Cacheable(debugInfo3, serviceProvider)
                            .ToList();
                        Assert.IsTrue(list3.Any());
                    }
                }
            });

            Task.WaitAll(task1, task2);

            Assert.AreEqual(debugInfo2.EFCacheKey.KeyHash, debugInfo3.EFCacheKey.KeyHash);
        }

        [TestMethod]
        public void TestSecondLevelCacheUsingDifferentSyncMethods()
        {
            var serviceProvider = TestsBase.GetServiceProvider();

            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product1";

                    Console.WriteLine("Count");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var count = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .Count();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsTrue(count > 0);


                    Console.WriteLine("ToList");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo1, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("FirstOrDefault");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var product1 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .FirstOrDefault();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.IsTrue(product1 != null);


                    Console.WriteLine("Any");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var any = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                       .Cacheable(debugInfo4, serviceProvider)
                                       .Any();
                    Assert.AreEqual(false, debugInfo4.IsCacheHit);
                    Assert.IsTrue(any);


                    Console.WriteLine("Sum");
                    var debugInfo5 = new EFCacheDebugInfo();
                    var sum = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                        .Cacheable(debugInfo5, serviceProvider)
                        .Sum(x => x.ProductId);
                    Assert.AreEqual(false, debugInfo5.IsCacheHit);
                    Assert.IsTrue(sum > 0);
                }
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheUsingTwoCountMethods()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product1";

                    Console.WriteLine("Count 1");

                    var debugInfo2 = new EFCacheDebugInfo();
                    var count = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .Count();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsTrue(count > 0);

                    Console.WriteLine("Count 2");
                    var debugInfo3 = new EFCacheDebugInfo();
                    count = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .Count();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(count > 0);
                }
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheUsingProjections()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product1";

                    Console.WriteLine("Projection 1");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Select(x => x.ProductId)
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());

                    Console.WriteLine("Projection 2");
                    var debugInfo3 = new EFCacheDebugInfo();
                    list2 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Select(x => x.ProductId)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .ToList();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list2.Any());
                }
            }
        }


        [TestMethod]
        public void TestSecondLevelCacheUsingFiltersAfterCacheableMethod()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("Filters After Cacheable Method 1.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var product1 = context.Products
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .FirstOrDefault(product => product.IsActive);
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsNotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 2, Same query.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .FirstOrDefault(product => product.IsActive);
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsNotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 3, Different query.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo4, serviceProvider)
                                       .FirstOrDefault(product => !product.IsActive);
                    Assert.AreEqual(false, debugInfo4.IsCacheHit);
                    Assert.IsNotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 4, Different query.");
                    var debugInfo5 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo5, serviceProvider)
                                       .FirstOrDefault(product => product.ProductName == "Product2");
                    Assert.AreEqual(false, debugInfo5.IsCacheHit);
                    Assert.IsNotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 5, Different query.");
                    var debugInfo6 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo6, serviceProvider)
                                       .FirstOrDefault(product => product.TagProducts.Any(tag => tag.TagId == 1));
                    Assert.AreEqual(false, debugInfo6.IsCacheHit);
                    Assert.IsNotNull(product1);
                }
            }
        }

        [TestMethod]
        public void TestEagerlyLoadingMultipleLevels()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("a normal query");
                    var product1IncludeTags = context.Users
                                                     .Include(x => x.Products)
                                                     .ThenInclude(x => x.TagProducts)
                                                     .ThenInclude(x => x.Tag)
                                                     .FirstOrDefault();
                    Assert.IsNotNull(product1IncludeTags);


                    Console.WriteLine("1st query using Include method.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var firstProductIncludeTags = context.Users
                                                         .Include(x => x.Products)
                                                         .ThenInclude(x => x.TagProducts)
                                                         .ThenInclude(x => x.Tag)
                                                         .Cacheable(debugInfo1, serviceProvider)
                                                         .FirstOrDefault();
                    Assert.IsNotNull(firstProductIncludeTags);
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;
                    var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;


                    Console.WriteLine("same cached query using Include method.");
                    var debugInfo11 = new EFCacheDebugInfo();
                    var firstProductIncludeTags11 = context.Users
                                                        .Include(x => x.Products)
                                                        .ThenInclude(x => x.TagProducts)
                                                        .ThenInclude(x => x.Tag)
                                                        .Cacheable(debugInfo11, serviceProvider)
                                                        .FirstOrDefault();
                    Assert.IsNotNull(firstProductIncludeTags11);
                    Assert.AreEqual(true, debugInfo11.IsCacheHit);


                    Console.WriteLine(
                        @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");

                    var debugInfo2 = new EFCacheDebugInfo();
                    var firstProduct = context.Users.Cacheable(debugInfo2, serviceProvider)
                                                   .FirstOrDefault();
                    Assert.IsNotNull(firstProduct);
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;
                    var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                    Assert.AreNotEqual(hash1, hash2);
                    Assert.AreNotEqual(cacheDependencies1, cacheDependencies2);
                }
            }
        }

        [TestMethod]
        public void TestIncludeMethodAndProjectionAffectsKeyCache()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("a normal query");
                    var product1IncludeTags = context.Products
                        .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                        .OrderBy(x => x.Name)
                        .FirstOrDefault();
                    Assert.IsNotNull(product1IncludeTags);
                }
            }

            string hash1;
            ISet<string> cacheDependencies1;
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st Cacheable query using Include method, reading from db");

                    var debugInfo1 = new EFCacheDebugInfo();
                    var firstProductIncludeTags = context.Products
                        .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                        .OrderBy(x => x.Name)
                        .Cacheable(debugInfo1, serviceProvider)
                        .FirstOrDefault();
                    Assert.IsNotNull(firstProductIncludeTags);
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    hash1 = debugInfo1.EFCacheKey.KeyHash;
                    cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;
                }
            }

            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("same Cacheable query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var firstProductIncludeTags2 = context.Products
                        .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                        .OrderBy(x => x.Name)
                        .Cacheable(debugInfo2, serviceProvider)
                        .FirstOrDefault();
                    Assert.IsNotNull(firstProductIncludeTags2);
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                }
            }

            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine(
                        @"3rd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");

                    var debugInfo3 = new EFCacheDebugInfo();
                    var firstProduct = context.Products
                        .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                        .OrderBy(x => x.Name)
                        .Cacheable(debugInfo3, serviceProvider)
                        .FirstOrDefault();
                    Assert.IsNotNull(firstProduct);
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;
                    var cacheDependencies3 = debugInfo3.EFCacheKey.CacheDependencies;

                    Assert.AreNotEqual(hash1, hash3);
                    Assert.AreNotEqual(cacheDependencies1, cacheDependencies3);
                }
            }
        }

        [TestMethod]
        public void TestParallelQueriesShouldCacheData()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            var debugInfo1 = new EFCacheDebugInfo();
            TestsBase.ExecuteInParallel(() =>
             {
                 using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                 {
                     using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                     {
                         var firstProductIncludeTags = context.Products
                             .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                             .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                             .OrderBy(x => x.Name)
                             .Cacheable(debugInfo1, serviceProvider)
                             .FirstOrDefault();
                         Assert.IsNotNull(firstProductIncludeTags);
                     }
                 }
             });
            Assert.AreEqual(true, debugInfo1.IsCacheHit);
        }

        [TestMethod]
        public void TestSecondLevelCacheUsingFindMethods()
        {
            var serviceProvider = TestsBase.GetServiceProvider();

            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var debugInfo = new EFCacheDebugInfo();
                    var product1 = context.Products
                        .Cacheable(debugInfo, serviceProvider)
                        .Find(1);
                    Assert.AreEqual(false, debugInfo.IsCacheHit);
                    Assert.IsTrue(product1 != null);


                    var debugInfo2 = new EFCacheDebugInfo();
                    product1 = context.Products
                        .Cacheable(debugInfo2, serviceProvider)
                        .Find(1);
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(product1 != null);
                }
            }
        }

        [TestMethod]
        public void TestNullValuesWillUseTheCache()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query, reading from db.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var item1 = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive && product.ProductName == "Product1xx")
                        .Cacheable(debugInfo1)
                        .FirstOrDefault();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsNull(item1);

                    Console.WriteLine("2nd query, reading from cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var item2 = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive && product.ProductName == "Product1xx")
                        .Cacheable(debugInfo2)
                        .FirstOrDefault();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsNull(item2);
                }
            }
        }

        [TestMethod]
        public void TestEqualsMethodWillUseTheCache()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query, reading from db.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var item1 = context.Products
                        .Where(product => product.ProductId == 2 && product.ProductName.Equals("Product1"))
                        .Cacheable(debugInfo1)
                        .FirstOrDefault();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsNotNull(item1);

                    Console.WriteLine("2nd query, reading from cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var item2 = context.Products
                        .Where(product => product.ProductId == 2 && product.ProductName.Equals("Product1"))
                        .Cacheable(debugInfo2)
                        .FirstOrDefault();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsNotNull(item2);

                    Console.WriteLine("3rd query, reading from db.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var item3 = context.Products
                        .Where(product => product.ProductId == 1 && product.ProductName.Equals("Product1"))
                        .Cacheable(debugInfo3)
                        .FirstOrDefault();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.IsNull(item3);
                }
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheDoesNotHitTheDatabaseForIQueryableCacheables()
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
                    var list1IQueryable = context.Products
                            .OrderBy(product => product.ProductNumber)
                            .Where(product => product.IsActive == isActive && product.ProductName == name) as IQueryable;
                    var list1 = (list1IQueryable.Cacheable(debugInfo1, serviceProvider) as IEnumerable).Cast<object>().ToList();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2IQueryable = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name) as IQueryable;
                    var list2 = (list2IQueryable.Cacheable(debugInfo2, serviceProvider) as IEnumerable).Cast<object>().ToList();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());
                }
            }
        }
    }
}