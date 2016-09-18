using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.Core.Tests
{
    [TestClass]
    public class EFCacheServiceProviderTests
    {

        [TestMethod]
        public void TestCacheInvalidationWithTwoRoots()
        {
            var cacheService = TestsBase.GetCacheServiceProvider();
            cacheService.InsertValue("EF_key1", "value1", new HashSet<string> { "entity1.model", "entity2.model" });

            cacheService.InsertValue("EF_key2", "value2", new HashSet<string> { "entity1.model", "entity2.model" });


            var value1 = cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            cacheService.InvalidateCacheDependencies(new[] { "entity2.model" });

            value1 = cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);
        }

        [TestMethod]
        public void TestCacheInvalidationWithOneRoot()
        {
            var cacheService = TestsBase.GetCacheServiceProvider();
            cacheService.InsertValue("EF_key1", "value1", new HashSet<string> { "entity1" });

            cacheService.InsertValue("EF_key2", "value2", new HashSet<string> { "entity1" });

            var value1 = cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            cacheService.InvalidateCacheDependencies(new[] { "entity1" });

            value1 = cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);
        }

        [TestMethod]
        public void TestCacheInvalidationWithSimilarRoots()
        {
            var cacheService = TestsBase.GetCacheServiceProvider();
            cacheService.InsertValue("EF_key1", "value1", new HashSet<string> { "entity1", "entity2" });

            cacheService.InsertValue("EF_key2", "value2", new HashSet<string> { "entity2" });

            var value1 = cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            cacheService.InvalidateCacheDependencies(new[] { "entity2" });

            value1 = cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);
        }

        [TestMethod]
        public void TestInsertingNullValues()
        {
            var cacheService = TestsBase.GetCacheServiceProvider();
            cacheService.InsertValue("EF_key1", null, new HashSet<string> { "entity1", "entity2" });

            var value1 = cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);
        }
    }
}