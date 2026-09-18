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
        public IActionResult Index()
        {
            Console.WriteLine("/Home/Index action method called");
            return View();
        }

        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            Console.WriteLine("/Home/Privacy action method called");
            return View();
        }


        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}
