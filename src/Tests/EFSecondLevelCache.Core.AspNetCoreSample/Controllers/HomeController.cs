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
    }
}