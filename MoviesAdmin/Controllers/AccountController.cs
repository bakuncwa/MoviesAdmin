using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoviesAdmin.Controllers
{
    // Sign-in/out through Auth0. There is no registration or password screen in this app: accounts
    // and their roles (administrator, critic) are created in the Auth0 Dashboard.
    public class AccountController : Controller
    {
        // GET: Account/Login?returnUrl=/Movies
        // Hands the browser to Auth0's hosted login page; Auth0 then returns it to /callback and on
        // to returnUrl. Any [Authorize] page does the same automatically for a signed-out visitor.
        public async Task Login(string returnUrl = "/")
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
                .WithRedirectUri(returnUrl)
                .Build();

            await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        }

        // POST: Account/Logout
        // Clears both the Auth0 session and this app's cookie, then comes back to the home page.
        // POST (with an antiforgery token) so another site can't sign the user out with a link.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Logout()
        {
            var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
                .WithRedirectUri(Url.Action("Index", "Home") ?? "/")
                .Build();

            await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // GET: Account/Profile
        // Who Auth0 says the signed-in user is, and which roles came through in the token.
        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        // GET: Account/AccessDenied
        // Where the cookie handler sends a signed-in user whose role doesn't allow the page.
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
