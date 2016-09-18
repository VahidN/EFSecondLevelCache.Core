using System;
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
    public class EFCachedQueryProviderAsyncTests
    {

        [TestMethod]
        public async Task TestSecondLevelCacheUsingAsyncMethodsDoesNotHitTheDatabase()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                                       .Cacheable(debugInfo1, serviceProvider)
                                       .ToListAsync();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;


                    Console.WriteLine("same async query, reading from 2nd level cache");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .ToListAsync();
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsTrue(list2.Any());
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;


                    Console.WriteLine("same async query, reading from 2nd level cache.");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .ToListAsync();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(list3.Any());
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;

                    Assert.AreEqual(hash1, hash2);
                    Assert.AreEqual(hash2, hash3);

                    Console.WriteLine("different async query, reading from db.");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var list4 = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                       .Cacheable(debugInfo4, serviceProvider)
                                       .ToListAsync();
                    Assert.AreEqual(false, debugInfo4.IsCacheHit);
                    Assert.IsTrue(list4.Any());

                    var hash4 = debugInfo4.EFCacheKey.KeyHash;
                    Assert.AreNotSame(hash3, hash4);

                    Console.WriteLine("different async query, reading from db.");
                    var debugInfo5 = new EFCacheDebugInfo();
                    var product1 = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                       .Cacheable(debugInfo5, serviceProvider)
                                       .FirstOrDefaultAsync();
                    Assert.AreEqual(false, debugInfo5.IsCacheHit);
                    Assert.IsNotNull(product1);

                    var hash5 = debugInfo5.EFCacheKey.KeyHash;
                    Assert.AreNotSame(hash4, hash5);
                }
            }
        }

        [TestMethod]
        public async Task TestSecondLevelCacheUsingDifferentAsyncMethods()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                                       .Cacheable(debugInfo1, serviceProvider)
                                       .ToListAsync();
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsTrue(list1.Any());


                    Console.WriteLine("CountAsync");
                    var debugInfo2 = new EFCacheDebugInfo();
                    var count = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .CountAsync();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsTrue(count > 0);


                    Console.WriteLine("FirstOrDefaultAsync");
                    var debugInfo3 = new EFCacheDebugInfo();
                    var product1 = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .FirstOrDefaultAsync();
                    Assert.AreEqual(false, debugInfo3.IsCacheHit);
                    Assert.IsTrue(product1 != null);


                    Console.WriteLine("AnyAsync");
                    var debugInfo4 = new EFCacheDebugInfo();
                    var any = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                       .Cacheable(debugInfo4, serviceProvider)
                                       .AnyAsync();
                    Assert.AreEqual(false, debugInfo4.IsCacheHit);
                    Assert.IsTrue(any);


                    Console.WriteLine("SumAsync");
                    var debugInfo5 = new EFCacheDebugInfo();
                    var sum = await context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                        .Cacheable(debugInfo5, serviceProvider)
                        .SumAsync(x => x.ProductId);
                    Assert.AreEqual(false, debugInfo5.IsCacheHit);
                    Assert.IsTrue(sum > 0);
                }
            }
        }

        [TestMethod]
        public async Task TestSecondLevelCacheUsingTwoCountAsyncMethods()
        {
            var serviceProvider = TestsBase.GetServiceProvider();
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
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
                                       .Cacheable(debugInfo2, serviceProvider)
                                       .CountAsync();
                    Assert.AreEqual(false, debugInfo2.IsCacheHit);
                    Assert.IsTrue(count > 0);

                    Console.WriteLine("Count 2, Reading from cache");
                    var debugInfo3 = new EFCacheDebugInfo();
                    count = await context.Products
                                       .OrderBy(product => product.ProductNumber)
                                       .Where(product => product.IsActive == isActive && product.ProductName == name)
                                       .Cacheable(debugInfo3, serviceProvider)
                                       .CountAsync();
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsTrue(count > 0);
                }
            }
        }
    }
}