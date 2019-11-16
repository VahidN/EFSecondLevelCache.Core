using System.Linq;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using Microsoft.AspNetCore.Mvc;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using EFSecondLevelCache.Core.Contracts;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using AutoMapper;
using EFSecondLevelCache.Core.AspNetCoreSample.Models;
using AutoMapper.QueryableExtensions;
using EFSecondLevelCache.Core.AspNetCoreSample.Others;

namespace EFSecondLevelCache.Core.AspNetCoreSample.Controllers
{
    public class HomeController : Controller
    {
        private readonly SampleContext _context;
        private readonly IMapper _mapper;
        private static List<Post> _inMemoryPosts;

        public HomeController(SampleContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            cacheInMemory();
        }

        private void cacheInMemory()
        {
            if (_inMemoryPosts == null)
            {
                _inMemoryPosts = _context.Set<Post>().AsNoTracking().ToList();
            }
        }

        /// <summary>
        /// Get https://localhost:5001/home/RunInMemory
        /// </summary>
        public IActionResult RunInMemory()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = _inMemoryPosts.AsQueryable()
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .Cacheable(debugInfo)
                .FirstOrDefault();
            return Json(new { post1?.Title, debugInfo });
        }

        public async Task<IActionResult> Index()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Set<Post>()
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo });
        }

        public async Task<IActionResult> TaskWhenAll()
        {
            var debugInfo = new EFCacheDebugInfo();
            var task1 = _context.Set<Post>().Where(x => x.Id > 0).Cacheable(debugInfo).ToListAsync();
            var results = await Task.WhenAll(task1);
            return Json(new { results, debugInfo });
        }

        public async Task<IActionResult> MapToDtoBefore()
        {
            var debugInfo = new EFCacheDebugInfo();
            var posts = await _context.Set<Post>()
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .ProjectTo<PostDto>(configuration: _mapper.ConfigurationProvider)
                .Cacheable(debugInfo)
                .ToListAsync();
            return Json(new { posts, debugInfo });
        }

        public async Task<IActionResult> MapToDtoAfter()
        {
            var debugInfo = new EFCacheDebugInfo();
            var posts = await _context.Set<Post>()
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .Cacheable(debugInfo)
                .ProjectTo<PostDto>(configuration: _mapper.ConfigurationProvider)
                .ToListAsync();
            return Json(new { posts, debugInfo });
        }

        /// <summary>
        /// Get https://localhost:5001/home/WithSlidingExpiration
        /// </summary>
        public async Task<IActionResult> WithSlidingExpiration()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Set<Post>()
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .Cacheable(new EFCachePolicy(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(5)), debugInfo)
                .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo });
        }

        /// <summary>
        /// Get https://localhost:5001/home/WithAbsoluteExpiration
        /// </summary>
        public async Task<IActionResult> WithAbsoluteExpiration()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Set<Post>()
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(5), debugInfo)
                .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo });
        }

        public async Task<IActionResult> AsyncTest()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Posts
                .Where(x => x.Id > 0)
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo });
        }

        public async Task<IActionResult> CountTest()
        {
            var debugInfo = new EFCacheDebugInfo();
            var count = await _context.Posts
                .Where(x => x.Id > 0)
                .Cacheable(debugInfo)
                .CountAsync();
            return Json(new { count, debugInfo });
        }

        public async Task<IActionResult> CountWithParamsTest()
        {
            var debugInfo = new EFCacheDebugInfo();
            var count = await _context.Posts
                .Cacheable(debugInfo)
                .CountAsync(x => x.Id > 0);
            return Json(new { count, debugInfo });
        }

        public async Task<IActionResult> CollectionsTest()
        {
            var collection1 = new[] { 1, 2, 3 };
            var debugInfo1 = new EFCacheDebugInfo();
            var post1 = await _context.Posts
                .Where(x => collection1.Contains(x.Id))
                .Cacheable(debugInfo1)
                .FirstOrDefaultAsync();

            var collection2 = new[] { 1, 2, 3, 4 };
            var debugInfo2 = new EFCacheDebugInfo();
            var post2 = await _context.Posts
                .Where(x => collection2.Contains(x.Id))
                .Cacheable(debugInfo2)
                .FirstOrDefaultAsync();
            return Json(new { post1.Title, post2.Id, debugInfo1, debugInfo2 });
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
            return Json(new { post1?.Title, debugInfo });
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
                ProductName = "P981122",
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

            // same query, reading from 2nd level cache? No.
            var debugInfo2 = new EFCacheDebugInfo();
            var secondQueryResult = _context.Products
                .Cacheable(debugInfo2)
                .FirstOrDefault(p => p.ProductId == product.ProductId);

            // same query, reading from 2nd level cache? No.
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

        public IActionResult DynamicGetWithCacheableAtFirst()
        {
            var debugInfo = new EFCacheDebugInfo();
            var users = _context.Users.DynamicGetWithCacheableAtFirst(debugInfo, x => x.Id > 0, x => x.Posts);
            return Json(new { users, debugInfo });
        }

        public IActionResult DynamicGetWithCacheableAtEnd()
        {
            var debugInfo = new EFCacheDebugInfo();
            var users = _context.Users.DynamicGetWithCacheableAtEnd(debugInfo, x => x.Id > 0, x => x.Posts);
            return Json(new { users, debugInfo }); // https://github.com/aspnet/EntityFrameworkCore/issues/12098
        }

        public async Task<IActionResult> FirstOrDefaultInline()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Set<Post>()
                .Where(x => x.Id == 1)
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo });
        }

        public async Task<IActionResult> FirstOrDefaultInlineAtTheEnd()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Set<Post>()
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync(x => x.Id == 1);
            return Json(new { post1.Title, debugInfo });
        }

        public async Task<IActionResult> FirstOrDefaultWithParam()
        {
            var param1 = 1;
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Set<Post>()
                .Where(x => x.Id == param1)
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo });
        }

        public async Task<IActionResult> FirstOrDefaultAtTheEndWithParam()
        {
            var param1 = 1;
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Set<Post>()
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync(x => x.Id == param1);
            return Json(new { post1.Title, debugInfo });
        }

        // https://localhost:5001/home/FirstOrDefaultWithParams
        public async Task<IActionResult> FirstOrDefaultWithParams()
        {
            var param1 = 1;
            var debugInfo1 = new EFCacheDebugInfo();
            var param2 = param1;
            var post1 = await _context.Set<Post>()
                .Where(x => x.Id == param2)
                .Cacheable(debugInfo1)
                .FirstOrDefaultAsync();

            param1 = 2;
            var debugInfo2 = new EFCacheDebugInfo();
            var post2 = await _context.Set<Post>()
                .Where(x => x.Id == param1)
                .Cacheable(debugInfo2)
                .FirstOrDefaultAsync();

            return Json(new { post1Title = post1.Title, debugInfo1, post2Title = post2.Title, debugInfo2 });
        }

        // https://github.com/VahidN/EFSecondLevelCache.Core/issues/53
        // https://localhost:5001/home/FirstOrDefaultWithParams2
        public async Task<IActionResult> FirstOrDefaultWithParams2()
        {
            var param1 = 1;
            var debugInfo1 = new EFCacheDebugInfo();
            var post1 = await _context.GetFirstOrDefaultAsync<Post, SampleContext>(debugInfo1, x => x.Id == param1);

            param1 = 2;
            var debugInfo2 = new EFCacheDebugInfo();
            var post2 = await _context.GetFirstOrDefaultAsync<Post, SampleContext>(debugInfo2, x => x.Id == param1);

            return Json(new { post1Title = post1.Title, debugInfo1, post2Title = post2.Title, debugInfo2 });
        }

        public async Task<IActionResult> TestEnumsWithParams()
        {
            var param1 = UserStatus.Active;
            var debugInfo = new EFCacheDebugInfo();
            var user1 = await _context.Set<User>()
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync(x => x.UserStatus == param1);
            return Json(new { user1, debugInfo });
        }

        public async Task<IActionResult> TestInlineEnums()
        {
            var debugInfo = new EFCacheDebugInfo();
            var user1 = await _context.Set<User>()
                .Cacheable(debugInfo)
                .FirstOrDefaultAsync(x => x.UserStatus == UserStatus.Active);
            return Json(new { user1, debugInfo });
        }

        public async Task<IActionResult> TestEFCachedDbSet()
        {
            var users1 = await _context.CachedPosts.OrderByDescending(x => x.Id).ToListAsync();

            var debugInfo2 = new EFCacheDebugInfo();
            var users2 = await _context.Set<Post>().Cacheable(debugInfo2).OrderByDescending(x => x.Id).ToListAsync();

            var debugInfo3 = new EFCacheDebugInfo();
            var users3 = await _context.Set<Post>().OrderByDescending(x => x.Id).Cacheable(debugInfo3).ToListAsync();
            return Json(new { users1, debugInfo2, debugInfo3});
        }
    }
}