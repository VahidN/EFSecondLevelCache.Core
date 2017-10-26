using System.Collections.Generic;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.Core.Tests
{
    [TestClass]
    public class EFCacheServiceProviderTests
    {
        private readonly IEFCacheServiceProvider _cacheService;
        public EFCacheServiceProviderTests()
        {
            _cacheService = TestsBase.GetInMemoryCacheServiceProvider();
            //_cacheService = TestsBase.GetRedisCacheServiceProvider();
        }

        [TestInitialize]
        public void ClearEFGlobalCacheBeforeEachTest()
        {
            _cacheService.ClearAllCachedEntries();
        }

        [TestMethod]
        public void TestCacheInvalidationWithTwoRoots()
        {
            _cacheService.InsertValue("EF_key1", "value1", new HashSet<string> { "entity1.model", "entity2.model" });

            _cacheService.InsertValue("EF_key2", "value2", new HashSet<string> { "entity1.model", "entity2.model" });


            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            _cacheService.InvalidateCacheDependencies(new[] { "entity2.model" });

            value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);
        }

        [TestMethod]
        public void TestCacheInvalidationWithOneRoot()
        {
            _cacheService.InsertValue("EF_key1", "value1", new HashSet<string> { "entity1" });

            _cacheService.InsertValue("EF_key2", "value2", new HashSet<string> { "entity1" });

            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            _cacheService.InvalidateCacheDependencies(new[] { "entity1" });

            value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);
        }

        [TestMethod]
        public void TestCacheInvalidationWithSimilarRoots()
        {
            _cacheService.InsertValue("EF_key1", "value1", new HashSet<string> { "entity1", "entity2" });

            _cacheService.InsertValue("EF_key2", "value2", new HashSet<string> { "entity2" });

            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            _cacheService.InvalidateCacheDependencies(new[] { "entity2" });

            value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);
        }

        [TestMethod]
        public void TestInsertingNullValues()
        {
            _cacheService.InsertValue("EF_key1", null, new HashSet<string> { "entity1", "entity2" });

            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsTrue(Equals(value1, EFCacheServiceProvider.NullObject), $"value1 is `{value1}`");
        }
    }
}