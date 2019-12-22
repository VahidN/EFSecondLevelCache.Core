using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.AspNetCoreSample.Models;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFSecondLevelCache.Core.Tests
{
    public class EFCachedQueryProviderBasicTests
    {
        private readonly IServiceProvider _serviceProvider;

        public EFCachedQueryProviderBasicTests()
        {
            _serviceProvider = TestsBase.GetServiceProvider();
            _serviceProvider.GetRequiredService<IEFCacheServiceProvider>().ClearAllCachedEntries();
        }

        [Fact]
        public void TestIncludeMethodAffectsKeyCache()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("a normal query");
                    var product1IncludeTags = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag).FirstOrDefault();
                    Assert.NotNull(product1IncludeTags);


                    Console.WriteLine("1st query using Include method.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var firstProductIncludeTags = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                                                                 .Cacheable(debugInfo1, _serviceProvider)
                                                                 .FirstOrDefault();
                    Assert.NotNull(firstProductIncludeTags);
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;
                    var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;

                    Console.WriteLine(
                        @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var firstProduct = context.Products.Cacheable(debugInfo2, _serviceProvider)
                                                      .FirstOrDefault();
                    Assert.NotNull(firstProduct);
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;
                    var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                    Assert.NotEqual(hash1, hash2);
                    Assert.NotEqual(cacheDependencies1, cacheDependencies2);
                }
            }
        }

        [Fact]
        public void TestQueriesUsingDifferentParameterValuesWillNotUseTheCache()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query, reading from db.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive && product.ProductName == "Product1")
                        .Cacheable(debugInfo1, _serviceProvider)
                        .ToList();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.NotNull(list1);

                    Console.WriteLine("2nd query, reading from db.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == false && product.ProductName == "Product1")
                        .Cacheable(debugInfo2, _serviceProvider)
                        .ToList();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.NotNull(list2);

                    Console.WriteLine("third query, reading from db.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == false && product.ProductName == "Product2")
                        .Cacheable(debugInfo3, _serviceProvider)
                        .ToList();
                    Assert.Equal(false, debugInfo3.IsCacheHit);
                    Assert.NotNull(list3);

                    Console.WriteLine("4th query, same as third one, reading from cache.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var list4 = context.Products.Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == false && product.ProductName == "Product2")
                        .Cacheable(debugInfo4, _serviceProvider)
                        .ToList();
                    Assert.Equal(true, debugInfo4.IsCacheHit);
                    Assert.NotNull(list4);
                }
            }
        }

        [Fact]
        public void TestSecondLevelCacheCreatesTheCommandTreeAfterCallingTheSameNormalQuery()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                    Assert.True(list1.Any());


                    Console.WriteLine("same query as Cacheable, reading from db.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, _serviceProvider)
                                       .ToList();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.True(list2.Any());
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, _serviceProvider)
                                       .ToList();
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.True(list3.Any());
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;

                    Assert.Equal(hash2, hash3);
                }
            }
        }

        [Fact]
        public void TestSecondLevelCacheDoesNotHitTheDatabase()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                                       .Cacheable(debugInfo1, _serviceProvider)
                                       .ToList();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.True(list1.Any());
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, _serviceProvider)
                                       .ToList();
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.True(list2.Any());
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, _serviceProvider)
                                       .ToList();
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.True(list3.Any());
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;

                    Assert.Equal(hash1, hash2);
                    Assert.Equal(hash2, hash3);

                    Console.WriteLine("different query, reading from db.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var list4 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                       .Cacheable(debugInfo4, _serviceProvider)
                                       .ToList();
                    Assert.Equal(false, debugInfo4.IsCacheHit);
                    Assert.True(list4.Any());

                    var hash4 = debugInfo4.EFCacheKey.KeyHash;
                    Assert.NotSame(hash3, hash4);
                }
            }
        }

        [Fact]
        public void TestSecondLevelCacheInTwoDifferentContextsDoesNotHitTheDatabase()
        {
            var isActive = true;
            var name = "Product1";
            string hash2;
            string hash3;

            Console.WriteLine("context 1.");
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query as Cacheable, reading from db.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, _serviceProvider)
                        .ToList();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.True(list2.Any());
                    hash2 = debugInfo2.EFCacheKey.KeyHash;
                }
            }

            Console.WriteLine("context 2");
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, _serviceProvider)
                                       .ToList();
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.True(list3.Any());
                    hash3 = debugInfo3.EFCacheKey.KeyHash;
                }
            }

            Assert.Equal(hash2, hash3);
        }


        [Fact]
        public void TestSecondLevelCacheInTwoDifferentParallelContexts()
        {
            var isActive = true;
            var name = "Product1";
            var debugInfo2 = new EFCacheDebugInfo();
            var debugInfo3 = new EFCacheDebugInfo();

            var task1 = Task.Factory.StartNew(() =>
            {
                Console.WriteLine("context 1.");
                using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                    {
                        Console.WriteLine("1st query as Cacheable.");
                        var list2 = context.Products
                            .OrderBy(product => product.ProductNumber)
                            .Where(product => product.IsActive == isActive && product.ProductName == name)
                            .Cacheable(debugInfo2, _serviceProvider)
                            .ToList();
                        Assert.True(list2.Any());
                    }
                }
            });

            var task2 = Task.Factory.StartNew(() =>
            {
                Console.WriteLine("context 2");
                using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                    {
                        Console.WriteLine("same query");
                        var list3 = context.Products
                            .OrderBy(product => product.ProductNumber)
                            .Where(product => product.IsActive == isActive && product.ProductName == name)
                            .Cacheable(debugInfo3, _serviceProvider)
                            .ToList();
                        Assert.True(list3.Any());
                    }
                }
            });

            Task.WaitAll(task1, task2);

            Assert.Equal(debugInfo2.EFCacheKey.KeyHash, debugInfo3.EFCacheKey.KeyHash);
        }

        [Fact]
        public void TestSecondLevelCacheUsingDifferentSyncMethods()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                                       .Cacheable(debugInfo2, _serviceProvider)
                                       .Count();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.True(count > 0);


                    Console.WriteLine("ToList");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo1, _serviceProvider)
                                       .ToList();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.True(list1.Any());


                    Console.WriteLine("FirstOrDefault");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var product1 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, _serviceProvider)
                                       .FirstOrDefault();
                    Assert.Equal(false, debugInfo3.IsCacheHit);
                    Assert.True(product1 != null);


                    Console.WriteLine("Any");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var any = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                       .Cacheable(debugInfo4, _serviceProvider)
                                       .Any();
                    Assert.Equal(false, debugInfo4.IsCacheHit);
                    Assert.True(any);


                    Console.WriteLine("Sum");
                    var debugInfo5 = new EFCacheDebugInfo();
                    var sum = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                        .Cacheable(debugInfo5, _serviceProvider)
                        .Sum(x => x.ProductId);
                    Assert.Equal(false, debugInfo5.IsCacheHit);
                    Assert.True(sum > 0);
                }
            }
        }

        [Fact]
        public void TestSecondLevelCacheUsingTwoCountMethods()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                                       .Cacheable(debugInfo2, _serviceProvider)
                                       .Count();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.True(count > 0);

                    Console.WriteLine("Count 2");
                    var debugInfo3 = new EFCacheDebugInfo();
                    count = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, _serviceProvider)
                                       .Count();
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.True(count > 0);
                }
            }
        }

        [Fact]
        public void TestSecondLevelCacheUsingProjections()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                                       .Cacheable(debugInfo2, _serviceProvider)
                                       .ToList();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.True(list2.Any());

                    Console.WriteLine("Projection 2");
                    var debugInfo3 = new EFCacheDebugInfo();
                    list2 = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Select(x => x.ProductId)
                                       .Cacheable(debugInfo3, _serviceProvider)
                                       .ToList();
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.True(list2.Any());
                }
            }
        }


        [Fact]
        public void TestSecondLevelCacheUsingFiltersAfterCacheableMethod()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("Filters After Cacheable Method 1.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var product1 = context.Products
                                       .Cacheable(debugInfo2, _serviceProvider)
                                       .FirstOrDefault(product => product.IsActive);
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.NotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 2, Same query.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo3, _serviceProvider)
                                       .FirstOrDefault(product => product.IsActive);
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.NotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 3, Different query.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo4, _serviceProvider)
                                       .FirstOrDefault(product => !product.IsActive);
                    Assert.Equal(false, debugInfo4.IsCacheHit);
                    Assert.NotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 4, Different query.");
                    var debugInfo5 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo5, _serviceProvider)
                                       .FirstOrDefault(product => product.ProductName == "Product2");
                    Assert.Equal(false, debugInfo5.IsCacheHit);
                    Assert.NotNull(product1);


                    Console.WriteLine("Filters After Cacheable Method 5, Different query.");
                    var debugInfo6 = new EFCacheDebugInfo();
                    product1 = context.Products
                                       .Cacheable(debugInfo6, _serviceProvider)
                                       .FirstOrDefault(product => product.TagProducts.Any(tag => tag.TagId == 1));
                    Assert.Equal(false, debugInfo6.IsCacheHit);
                    Assert.NotNull(product1);
                }
            }
        }

        [Fact]
        public void TestEagerlyLoadingMultipleLevels()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("a normal query");
                    var product1IncludeTags = context.Users
                                                     .Include(x => x.Products)
                                                     .ThenInclude(x => x.TagProducts)
                                                     .ThenInclude(x => x.Tag)
                                                     .FirstOrDefault();
                    Assert.NotNull(product1IncludeTags);


                    Console.WriteLine("1st query using Include method.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var firstProductIncludeTags = context.Users
                                                         .Include(x => x.Products)
                                                         .ThenInclude(x => x.TagProducts)
                                                         .ThenInclude(x => x.Tag)
                                                         .Cacheable(debugInfo1, _serviceProvider)
                                                         .FirstOrDefault();
                    Assert.NotNull(firstProductIncludeTags);
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;
                    var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;


                    Console.WriteLine("same cached query using Include method.");
                    var debugInfo11 = new EFCacheDebugInfo();
                    var firstProductIncludeTags11 = context.Users
                                                        .Include(x => x.Products)
                                                        .ThenInclude(x => x.TagProducts)
                                                        .ThenInclude(x => x.Tag)
                                                        .Cacheable(debugInfo11, _serviceProvider)
                                                        .FirstOrDefault();
                    Assert.NotNull(firstProductIncludeTags11);
                    Assert.Equal(true, debugInfo11.IsCacheHit);


                    Console.WriteLine(
                        @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");

                    var debugInfo2 = new EFCacheDebugInfo();
                    var firstProduct = context.Users.Cacheable(debugInfo2, _serviceProvider)
                                                   .FirstOrDefault();
                    Assert.NotNull(firstProduct);
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;
                    var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                    Assert.NotEqual(hash1, hash2);
                    Assert.NotEqual(cacheDependencies1, cacheDependencies2);
                }
            }
        }

        [Fact]
        public void TestIncludeMethodAndProjectionAffectsKeyCache()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("a normal query");
                    var product1IncludeTags = context.Products
                        .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                        .OrderBy(x => x.Name)
                        .FirstOrDefault();
                    Assert.NotNull(product1IncludeTags);
                }
            }

            string hash1;
            ISet<string> cacheDependencies1;
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st Cacheable query using Include method, reading from db");

                    var debugInfo1 = new EFCacheDebugInfo();
                    var firstProductIncludeTags = context.Products
                        .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                        .OrderBy(x => x.Name)
                        .Cacheable(debugInfo1, _serviceProvider)
                        .FirstOrDefault();
                    Assert.NotNull(firstProductIncludeTags);
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    hash1 = debugInfo1.EFCacheKey.KeyHash;
                    cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;
                }
            }

            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("same Cacheable query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var firstProductIncludeTags2 = context.Products
                        .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                        .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                        .OrderBy(x => x.Name)
                        .Cacheable(debugInfo2, _serviceProvider)
                        .FirstOrDefault();
                    Assert.NotNull(firstProductIncludeTags2);
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                }
            }

            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                        .Cacheable(debugInfo3, _serviceProvider)
                        .FirstOrDefault();
                    Assert.NotNull(firstProduct);
                    Assert.Equal(false, debugInfo3.IsCacheHit);
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;
                    var cacheDependencies3 = debugInfo3.EFCacheKey.CacheDependencies;

                    Assert.NotEqual(hash1, hash3);
                    Assert.NotEqual(cacheDependencies1, cacheDependencies3);
                }
            }
        }

        [Fact]
        public void TestParallelQueriesShouldCacheData()
        {
            var debugInfo1 = new EFCacheDebugInfo();
            TestsBase.ExecuteInParallel(() =>
             {
                 using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                 {
                     using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                     {
                         var firstProductIncludeTags = context.Products
                             .Include(x => x.TagProducts).ThenInclude(x => x.Tag)
                             .Select(x => new { Name = x.ProductName, Tag = x.TagProducts.Select(y => y.Tag) })
                             .OrderBy(x => x.Name)
                             .Cacheable(debugInfo1, _serviceProvider)
                             .FirstOrDefault();
                         Assert.NotNull(firstProductIncludeTags);
                     }
                 }
             });
            Assert.Equal(true, debugInfo1.IsCacheHit);
        }

        [Fact]
        public void TestSecondLevelCacheUsingFindMethods()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var debugInfo = new EFCacheDebugInfo();
                    var product1 = context.Products
                        .Cacheable(debugInfo, _serviceProvider)
                        .Find(1);
                    Assert.Equal(false, debugInfo.IsCacheHit);
                    Assert.True(product1 != null);


                    var debugInfo2 = new EFCacheDebugInfo();
                    product1 = context.Products
                        .Cacheable(debugInfo2, _serviceProvider)
                        .Find(1);
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.True(product1 != null);
                }
            }
        }

        [Fact]
        public void TestNullValuesWillUseTheCache()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.Null(item1);

                    Console.WriteLine("2nd query, reading from cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var item2 = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive && product.ProductName == "Product1xx")
                        .Cacheable(debugInfo2)
                        .FirstOrDefault();
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.Null(item2);
                }
            }
        }

        [Fact]
        public void TestEqualsMethodWillUseTheCache()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    Console.WriteLine("1st query, reading from db.");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var item1 = context.Products
                        .Where(product => product.ProductId == 2 && product.ProductName.Equals("Product1"))
                        .Cacheable(debugInfo1)
                        .FirstOrDefault();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.NotNull(item1);

                    Console.WriteLine("2nd query, reading from cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var item2 = context.Products
                        .Where(product => product.ProductId == 2 && product.ProductName.Equals("Product1"))
                        .Cacheable(debugInfo2)
                        .FirstOrDefault();
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.NotNull(item2);

                    Console.WriteLine("3rd query, reading from db.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var item3 = context.Products
                        .Where(product => product.ProductId == 1 && product.ProductName.Equals("Product1"))
                        .Cacheable(debugInfo3)
                        .FirstOrDefault();
                    Assert.Equal(false, debugInfo3.IsCacheHit);
                    Assert.Null(item3);
                }
            }
        }

        [Fact]
        public void TestSecondLevelCacheDoesNotHitTheDatabaseForIQueryableCacheables()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                    var list1 = (list1IQueryable.Cacheable(debugInfo1, _serviceProvider) as IEnumerable).Cast<object>().ToList();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.True(list1.Any());


                    Console.WriteLine("same query, reading from 2nd level cache.");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2IQueryable = context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name) as IQueryable;
                    var list2 = (list2IQueryable.Cacheable(debugInfo2, _serviceProvider) as IEnumerable).Cast<object>().ToList();
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.True(list2.Any());
                }
            }
        }

        [Fact]
        public void Test2DifferentCollectionsWillNotUseTheCache()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var collection1 = new[] { 1, 2, 3 };
                    var debugInfo1 = new EFCacheDebugInfo();
                    var item1 = context.Products
                        .Where(product => collection1.Contains(product.ProductId))
                        .Cacheable(debugInfo1)
                        .FirstOrDefault();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.NotNull(item1);

                    var collection2 = new[] { 1, 2, 3, 4 };
                    var debugInfo2 = new EFCacheDebugInfo();
                    var item2 = context.Products
                        .Where(product => collection2.Contains(product.ProductId))
                        .Cacheable(debugInfo2)
                        .FirstOrDefault();
                    Assert.Equal(false, debugInfo2.IsCacheHit); // Works with `RelationalQueryModelVisitor`
                    Assert.NotNull(item2);
                }
            }
        }

        [Fact]
        //[ExpectedException(typeof(InvalidOperationException))] // This is a bug or a limitation in EF Core
        public void TestSubqueriesWillUseTheCache()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var debugInfo1 = new EFCacheDebugInfo();

                    var queryable = context.Products.Select(product => new
                    {
                        prop1 = product.UserId,
                        prop2 = context.TagProducts.Where(tag => tag.ProductProductId == product.ProductId)
                            .Cacheable().Select(tag => new
                            {
                                tag.TagId
                            })
                    });
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        var item1 = queryable.FirstOrDefault();
                        Assert.Equal(false, debugInfo1.IsCacheHit);
                        Assert.NotNull(item1);
                    });

                    var debugInfo2 = new EFCacheDebugInfo();

                    Assert.Throws<InvalidOperationException>(() => 
                    {
                        var item2 = queryable.FirstOrDefault();
                        Assert.Equal(false, debugInfo2.IsCacheHit);
                        Assert.NotNull(item2); 
                    });
                }
            }
        }

        [Fact]
        public void TestSecondLevelCacheUsingProjectToMethods()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var mapper = serviceScope.ServiceProvider.GetRequiredService<IMapper>();
                    var debugInfo = new EFCacheDebugInfo();
                    var posts = context.Posts
                        .Where(x => x.Id > 0)
                        .OrderBy(x => x.Id)
                        .Cacheable(debugInfo, _serviceProvider)
                        .ProjectTo<PostDto>(configuration: mapper.ConfigurationProvider)
                        .ToList();
                    Assert.Equal(false, debugInfo.IsCacheHit);
                    Assert.True(posts != null);

                    var debugInfo2 = new EFCacheDebugInfo();
                    posts = context.Posts
                        .Where(x => x.Id > 0)
                        .OrderBy(x => x.Id)
                        .Cacheable(debugInfo2, _serviceProvider)
                        .ProjectTo<PostDto>(configuration: mapper.ConfigurationProvider)
                        .ToList();
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.True(posts != null);
                }
            }
        }
    }
}