using System.Linq;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using Microsoft.AspNetCore.Mvc;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using EFSecondLevelCache.Core.Contracts;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;

namespace EFSecondLevelCache.Core.AspNetCoreSample.Controllers
{
    public class HomeController : Controller
    {
        private readonly SampleContext _context;

        public HomeController(SampleContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = _context.Set<Post>()
                                .Where(x => x.Id > 0)
                                .OrderBy(x => x.Id)
                                .Cacheable(debugInfo)
                                .FirstOrDefault();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }

        /// <summary>
        /// Get https://localhost:5001/home/WithSlidingExpiration
        /// </summary>
        public IActionResult WithSlidingExpiration()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = _context.Set<Post>()
                                .Where(x => x.Id > 0)
                                .OrderBy(x => x.Id)
                                .Cacheable(new EFCachePolicy(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(5)), debugInfo)
                                .FirstOrDefault();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }

        /// <summary>
        /// Get https://localhost:5001/home/WithAbsoluteExpiration
        /// </summary>
        public IActionResult WithAbsoluteExpiration()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = _context.Set<Post>()
                                .Where(x => x.Id > 0)
                                .OrderBy(x => x.Id)
                                .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(5), debugInfo)
                                .FirstOrDefault();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }

        public async Task<IActionResult> AsyncTest()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Posts
                                      .Where(x => x.Id > 0)
                                      .Cacheable(debugInfo)
                                      .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }

        public async Task<IActionResult> CollectionsTest()
        {
            var collection1 = new[] { 1, 2, 3 };
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Posts
                                      .Where(x => collection1.Contains(x.Id))
                                      .Cacheable(debugInfo)
                                      .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }

        /// <summary>
        /// Get https://localhost:5001/home/StringEqualsTest
        /// </summary>
        public async Task<IActionResult> StringEqualsTest()
        {
            var debugInfo = new EFCacheDebugInfo();
            var rnd = new Random();
            var value = rnd.Next(1, 1000000).ToString();
            var post1 = await _context.Posts
                                      .Where(x => x.Title.Equals(value))
                                      .Cacheable(debugInfo)
                                      .FirstOrDefaultAsync();
            return Json(new { value, debugInfo.IsCacheHit });
        }

        /// <summary>
        /// Get https://localhost:5001/home/Issue36
        /// </summary>
        public IActionResult Issue36()
        {
            User user1;
            const string user1Name = "User1";
            if (!_context.Users.Any(user => user.Name == user1Name))
            {
                user1 = new User { Name = user1Name };
                user1 = _context.Users.Add(user1).Entity;
            }
            else
            {
                user1 = _context.Users.First(user => user.Name == user1Name);
            }

            var product = new Product
            {
                ProductName = "P98112",
                IsActive = true,
                Notes = "Notes ...",
                ProductNumber = "098112",
                User = user1
            };

            product = _context.Products.Add(product).Entity;
            _context.SaveChanges();

            // 1st query, reading from db
            var debugInfo1 = new EFCacheDebugInfo();
            var firstQueryResult = _context.Products
                             .Cacheable(debugInfo1)
                             .FirstOrDefault(p => p.ProductId == product.ProductId);

            var debugInfoWithWhere1 = new EFCacheDebugInfo();
            var firstQueryWithWhereClauseResult = _context.Products.Where(p => p.ProductId == product.ProductId)
                            .Cacheable(debugInfoWithWhere1)
                            .FirstOrDefault();

            // Delete it from db, invalidates the cache on SaveChanges
            _context.Products.Remove(product);
            _context.SaveChanges();

            // same query, reading from 2nd level cache? Yes. Because its ToSQL() has no where clause yet!
            // The ToSql() method (which will be used to calculate the hash of the query or the cache key automatically) doesn't see the x => x.ID == a.ID predicate. It will be evaluated where the Cacheable(debugger) method is located (It doesn't see anything after it).
            var debugInfo2 = new EFCacheDebugInfo();
            var secondQueryResult = _context.Products
                         .Cacheable(debugInfo2)
                         .FirstOrDefault(p => p.ProductId == product.ProductId);

            // same query, reading from 2nd level cache? No. Because its ToSQL() has a where clause.
            var debugInfo3 = new EFCacheDebugInfo();
            var thirdQueryResult = _context.Products.Where(p => p.ProductId == product.ProductId)
                         .Cacheable(debugInfo3)
                         .FirstOrDefault();

            // retrieving it directly from database
            var p98 = _context.Products.FirstOrDefault(p => p.ProductId == product.ProductId);

            return Json(new
            {
                firstQueryResult,
                isFirstQueryCached = debugInfo1,

                firstQueryWithWhereClauseResult,
                isFirstQueryWithWhereClauseCached = debugInfoWithWhere1,

                secondQueryResult,
                isSecondQueryCached = debugInfo2,

                thirdQueryResult,
                isThirdQueryCached = debugInfo3,

                directlyFromDatabase = p98
            });
        }
    }
}