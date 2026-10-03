using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    public class Review
    {
        // Primary key.
        public int Id { get; set; }

        // FK to the movie being reviewed.
        public int MovieId { get; set; }
        // Navigation to the movie itself.
        public Movie Movie { get; set; } = null!;

        // FK to the reviewer's Identity user id (ApplicationUser.Id, a string key).
        [Required(ErrorMessage = "A review must belong to a user.")]
        [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "UserId must be a valid Identity key (letters, numbers, and hyphens only).")]
        public string UserId { get; set; } = string.Empty;
        // Navigation to the reviewer.
        public ApplicationUser User { get; set; } = null!;

        // Star rating on a 1-5 scale; feeds Movie.AverageRating.
        [Range(1, 5, ErrorMessage = "Rating must be between {1} and {2} stars.")]
        public int Rating { get; set; }

        // Optional free-text review comment.
        [StringLength(2000, ErrorMessage = "Comment can be at most {1} characters.")]
        [RegularExpression(@"^[^<>]*$", ErrorMessage = "Comment cannot contain '<' or '>' characters.")]
        public string? Comment { get; set; }

        // When the review was submitted; defaults to now at creation time.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
