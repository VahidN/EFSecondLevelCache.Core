﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFSecondLevelCache.Core.Tests
{
    public class EFCachedQueryProviderAsyncTests
    {
        private readonly IServiceProvider _serviceProvider;

        public EFCachedQueryProviderAsyncTests()
        {
            _serviceProvider = TestsBase.GetServiceProvider();
            _serviceProvider.GetRequiredService<IEFCacheServiceProvider>().ClearAllCachedEntries();
        }

        [Fact]
        public async Task TestSecondLevelCacheUsingAsyncMethodsDoesNotHitTheDatabase()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product1";

                    Console.WriteLine("1st async query, reading from db");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo1, _serviceProvider)
                        .ToListAsync();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.True(list1.Any());
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;


                    Console.WriteLine("same async query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, _serviceProvider)
                        .ToListAsync();
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.True(list2.Any());
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;


                    Console.WriteLine("same async query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, _serviceProvider)
                        .ToListAsync();
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.True(list3.Any());
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;

                    Assert.Equal(hash1, hash2);
                    Assert.Equal(hash2, hash3);

                    Console.WriteLine("different async query, reading from db.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var list4 = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                        .Cacheable(debugInfo4, _serviceProvider)
                        .ToListAsync();
                    Assert.Equal(false, debugInfo4.IsCacheHit);
                    Assert.True(list4.Any());

                    var hash4 = debugInfo4.EFCacheKey.KeyHash;
                    Assert.NotSame(hash3, hash4);

                    Console.WriteLine("different async query, reading from db.");
                    var debugInfo5 = new EFCacheDebugInfo();
                    var product1 = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                        .Cacheable(debugInfo5, _serviceProvider)
                        .FirstOrDefaultAsync();
                    Assert.Equal(false, debugInfo5.IsCacheHit);
                    Assert.NotNull(product1);

                    var hash5 = debugInfo5.EFCacheKey.KeyHash;
                    Assert.NotSame(hash4, hash5);
                }
            }
        }

        [Fact]
        public async Task TestSecondLevelCacheUsingDifferentAsyncMethods()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product3";

                    Console.WriteLine("ToListAsync");
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo1, _serviceProvider)
                        .ToListAsync();
                    Assert.Equal(false, debugInfo1.IsCacheHit);
                    Assert.True(list1.Any());


                    Console.WriteLine("CountAsync");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var count = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, _serviceProvider)
                        .CountAsync();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.True(count > 0);


                    Console.WriteLine("FirstOrDefaultAsync");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var product1 = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, _serviceProvider)
                        .FirstOrDefaultAsync();
                    Assert.Equal(false, debugInfo3.IsCacheHit);
                    Assert.True(product1 != null);


                    Console.WriteLine("AnyAsync");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var any = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                        .Cacheable(debugInfo4, _serviceProvider)
                        .AnyAsync();
                    Assert.Equal(false, debugInfo4.IsCacheHit);
                    Assert.True(any);


                    Console.WriteLine("SumAsync");
                    var debugInfo5 = new EFCacheDebugInfo();
                    var sum = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                        .Cacheable(debugInfo5, _serviceProvider)
                        .SumAsync(x => x.ProductId);
                    Assert.Equal(false, debugInfo5.IsCacheHit);
                    Assert.True(sum > 0);
                }
            }
        }

        [Fact]
        public async Task TestSecondLevelCacheUsingTwoCountAsyncMethods()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var isActive = true;
                    var name = "Product2";

                    Console.WriteLine("Count 1, From DB");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var count = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2, _serviceProvider)
                        .CountAsync();
                    Assert.Equal(false, debugInfo2.IsCacheHit);
                    Assert.True(count > 0);

                    Console.WriteLine("Count 2, Reading from cache");
                    var debugInfo3 = new EFCacheDebugInfo();
                    count = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3, _serviceProvider)
                        .CountAsync();
                    Assert.Equal(true, debugInfo3.IsCacheHit);
                    Assert.True(count > 0);
                }
            }
        }

        [Fact]
        public async Task TestSecondLevelCacheUsingFindAsyncMethods()
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var debugInfo = new EFCacheDebugInfo();
                    var product1 = await context.Products
                        .Cacheable(debugInfo, _serviceProvider)
                        .FindAsync(1);
                    Assert.Equal(false, debugInfo.IsCacheHit);
                    Assert.True(product1 != null);


                    var debugInfo2 = new EFCacheDebugInfo();
                    product1 = await context.Products
                        .Cacheable(debugInfo2, _serviceProvider)
                        .FindAsync(1);
                    Assert.Equal(true, debugInfo2.IsCacheHit);
                    Assert.True(product1 != null);
                }
            }
        }
        
        // Ignore
        public void TestParallelAsyncCalls()
        {
            var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SampleContext>();

            var tests = new List<Action>();
            for (var i = 0; i < 4000; i++)
            {
                var i1 = i.ToString();
                tests.Add(async () =>
                {
                    var count = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive && product.ProductName == i1)
                        .Cacheable(_serviceProvider)
                        .CountAsync();
                });
            }

            var rnd = new Random();
            Parallel.Invoke(tests.OrderBy(a => rnd.Next()).ToArray());
        }
    }
}