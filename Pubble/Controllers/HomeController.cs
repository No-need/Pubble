using Microsoft.AspNetCore.Mvc;
using Pubble.Models;
using System.Diagnostics;

namespace Pubble.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Pubble()
        {
            return View();
        }

        public IActionResult Pubble2([FromKeyedServices("Pubble")] Game game)
        {
            return View(game);
        }

        public IActionResult Control([FromKeyedServices("Pubble")] Game game)
        {
            return View(game);
        }
    }
}
