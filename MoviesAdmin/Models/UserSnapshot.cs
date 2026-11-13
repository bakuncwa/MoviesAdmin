using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    // A local copy of an Auth0 user's profile, written at every login by UserSnapshotService. The
    // accounts themselves live in Auth0; this table lets the database show who reviews belong to
    // (Review.CreatedBy matches UserId) without calling Auth0.
    public class UserSnapshot
    {
        // Primary key.
        public int Id { get; set; }

        // Auth0 user id (the "sub" claim, e.g. "auth0|6543..."); unique (see ApplicationDbContext),
        // so each user has exactly one row that later logins update.
        [Required]
        [StringLength(255)]
        public string UserId { get; set; } = string.Empty;

        // Display name from Auth0's "name" claim (Auth0 falls back to the email when none is set).
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(256)]
        public string? Email { get; set; }

        // "administrator", "critic", or both comma-separated, as the login's roles claim listed them.
        [StringLength(100)]
        public string? Roles { get; set; }

        // When the row was created, i.e. the user's first login to this app.
        public DateTime FirstLoginAt { get; set; } = DateTime.UtcNow;

        // Refreshed on every login.
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }
}
