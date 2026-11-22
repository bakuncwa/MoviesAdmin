using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesAdmin.Models;
using System.Diagnostics;

namespace MoviesAdmin.Controllers
{
    public class HomeController : Controller
    {
        // Constructor
        public HomeController()
        {
        }

        //Action methods

        // GET: Home/Index
        // The one page open to signed-out visitors: what the app is, plus a link to log in.
        public IActionResult Index()
        {
            return View();
        }

        // GET: Home/Privacy
        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        // Home/Error
        // Where UseExceptionHandler("/Home/Error") in Program.cs sends unhandled exceptions outside
        // Development. Open to everyone (a failed login has no signed-in user) and never cached.
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
