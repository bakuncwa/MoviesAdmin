using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;

namespace MoviesAdmin.Services.UserSnapshots
{
    // Adds or updates the signed-in user's UserSnapshot row. Called from the OpenID Connect
    // OnTokenValidated event in Program.cs, i.e. once per login, after Auth0's id token has been
    // validated and before the auth cookie is written.
    public class UserSnapshotService
    {
        private readonly ApplicationDbContext _context;

        public UserSnapshotService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordLoginAsync(ClaimsPrincipal user)
        {
            var userId = user.GetUserId();
            // The OIDC handler maps "email" to ClaimTypes.Email; "name" stays as it is.
            var name = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name);
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
            var roles = new[] { Roles.Administrator, Roles.Critic }.Where(user.IsInRole).ToList();

            var snapshot = await _context.UserSnapshots.SingleOrDefaultAsync(u => u.UserId == userId);
            if (snapshot is null)
            {
                snapshot = new UserSnapshot { UserId = userId };
                _context.UserSnapshots.Add(snapshot);
            }
            else
            {
                snapshot.LastLoginAt = DateTime.UtcNow;
            }

            snapshot.Name = name;
            snapshot.Email = email;
            snapshot.Roles = roles.Count > 0 ? string.Join(",", roles) : null;

            await _context.SaveChangesAsync();
        }
    }
}
