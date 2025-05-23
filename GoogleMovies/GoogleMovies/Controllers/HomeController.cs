using GoogleMovies.Data;
using GoogleMovies.Models;
using GoogleMovies.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GoogleMovies.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly MovieDbContext _context;

        public HomeController(ILogger<HomeController> logger, MovieDbContext context, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Customer")]
        //public IActionResult Index()
        //{
        //    return View();
        //}

        public async Task<IActionResult> Index()
        {
            var topPicks = await _context.Movies
                .Where(m => m.IsTrending)
                .OrderByDescending(m => m.ModifiedDate)
                .Take(10)
                .ToListAsync();

            var blockbusters = await _context.Movies
                .Where(m => m.BoxOffice >= 350_000_000)
                .OrderByDescending(m => m.BoxOffice)
                .Take(10)
                .ToListAsync();

            var latestMovies = await _context.Movies
                .Where(m => m.Year == 2025)
                .OrderByDescending(m => m.ModifiedDate)
                .Take(10)
                .ToListAsync();

            var viewModel = new HomeViewModel
            {
                Trending = topPicks,
                Blockbusters = blockbusters,
                LatestMovies = latestMovies
            };

            return View(viewModel);
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
    }
}
