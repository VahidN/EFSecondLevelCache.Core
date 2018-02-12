using System.Linq;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using Microsoft.AspNetCore.Mvc;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using EFSecondLevelCache.Core.Contracts;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
            var post1 = _context.Set<Post>().OrderBy(x => x.Id).Cacheable(debugInfo).FirstOrDefault();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }

        public async Task<IActionResult> AsyncTest()
        {
            var debugInfo = new EFCacheDebugInfo();
            var post1 = await _context.Posts.Where(x => x.Id > 0).Cacheable(debugInfo).FirstOrDefaultAsync();
            return Json(new { post1.Title, debugInfo.IsCacheHit });
        }
    }
}