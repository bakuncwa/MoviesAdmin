using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MoviesAdmin.Models
{
    // Extends the default Identity user so admin screens can show a friendly name.
    public class ApplicationUser : IdentityUser
    {
        // Friendly name shown in the admin UI (e.g. navbar greeting); everything else (Id, Email,
        // UserName, PasswordHash, ...) is inherited from IdentityUser.
        [RegularExpression(@"^[\p{L}\p{N}\s\-'.]*$", ErrorMessage = "Display name can only contain letters, numbers, spaces, hyphens, apostrophes, and periods.")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
