using System.Linq;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using Microsoft.AspNetCore.Mvc;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using EFSecondLevelCache.Core.Contracts;

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
            //var post1 = _context.Posts.Where(x => x.Id > 0).Cacheable(debugInfo).FirstOrDefault();
            var post1 = _context.Set<Post>().Cacheable(debugInfo).FirstOrDefault();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }
    }
}