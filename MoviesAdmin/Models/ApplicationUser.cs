using Microsoft.AspNetCore.Identity;

namespace MoviesAdmin.Models
{
    // Extends the default Identity user so admin screens can show a friendly name.
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
