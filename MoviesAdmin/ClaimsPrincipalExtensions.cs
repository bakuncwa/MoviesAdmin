using System.Security.Claims;

namespace MoviesAdmin
{
    // User.GetUserId() returns the signed-in Auth0 user's id (the "sub" claim, e.g.
    // "auth0|6543..."), which Review.CreatedBy stores so critics only ever see their own reviews.
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User is not authenticated");
    }
}
