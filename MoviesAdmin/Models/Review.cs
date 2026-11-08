using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MoviesAdmin.Models
{
    // A critic's review of one movie. Starts as a draft (editable/deletable by its author) and,
    // once published, is locked for good: ReviewsController refuses to edit or delete it.
    public class Review
    {
        // Primary key.
        public int Id { get; set; }

        // FK to the movie being reviewed.
        [Range(1, int.MaxValue, ErrorMessage = "Choose a movie.")]
        [Display(Name = "Movie")]
        public int MovieId { get; set; }
        // Navigation to the movie itself.
        public Movie Movie { get; set; } = null!;

        // Auth0 user id of the critic who wrote it (User.GetUserId(), e.g. "auth0|6543..."). Users
        // live in Auth0, not this database, so this is a plain string rather than a foreign key.
        // Unique together with MovieId: one review per movie per critic.
        [StringLength(255)]
        public string CreatedBy { get; set; } = string.Empty;

        // Critic's name as Auth0 reported it when the review was created, for display only.
        [StringLength(200)]
        public string? CriticName { get; set; }

        // Rating out of 5 in half-star steps (0, 0.5, 1 ... 5); feeds Movie.AverageRating once published.
        [Range(0, 5, ErrorMessage = "Rating must be between {1} and {2}.")]
        [Precision(2, 1)]
        public decimal Rating { get; set; }

        // The review itself, as sanitized HTML from the review editor (see ReviewHtml). Its visible
        // text is at most ReviewHtml.MaxTextLength (1000) characters, checked by ReviewsController;
        // the column itself is unbounded because the formatting markup adds to the length.
        [Required(ErrorMessage = "Review text is required.")]
        [Display(Name = "Review")]
        public string Text { get; set; } = string.Empty;

        // False while a draft; set once by ReviewsController.Publish and never cleared.
        public bool IsPublished { get; set; }

        // When the review was first saved; defaults to now at creation time.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // When the draft was last edited, if it has been.
        public DateTime? UpdatedAt { get; set; }

        // When it was published; null while a draft.
        public DateTime? PublishedAt { get; set; }
    }
}
