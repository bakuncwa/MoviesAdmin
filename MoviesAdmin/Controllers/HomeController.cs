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


        // Using ContentResult to return a simple string response
        public JsonResult GetMovie()
        {
            modelMovie movie = new modelMovie();
            movie.Id = 1;
            movie.Title = "Pride & Prejudice";
            movie.Synopsis =
                "When Elizabeth Bennet meets the handsome Mr. Darcy, she believes he is the last man she could ever marry, " +
                "but as their lives become intertwined, she finds herself captivated by the man she has sworn to hate forever.\r\n\r\n";
            return Json(movie);
        }


        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}
