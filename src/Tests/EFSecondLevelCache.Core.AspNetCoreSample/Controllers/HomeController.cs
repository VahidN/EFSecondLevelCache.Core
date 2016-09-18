using System.Linq;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer;
using Microsoft.AspNetCore.Mvc;

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
            var post1 = _context.Posts.Cacheable().FirstOrDefault();
            return Json(new { title = post1.Title });
        }
    }
}