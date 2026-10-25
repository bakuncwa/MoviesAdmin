using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    // MPAA-style audience rating (the "PG-13" kind of rating), distinct from the 1-5 star review
    // average. One-to-many: an audience rating can have many movies, and a movie belongs to one
    // audience rating (Movie.AudienceRatingId).
    public class AudienceRating
    {
        // Primary key.
        public int Id { get; set; }

        // Rating label as shown in the UI (e.g. "PG-13"); unique, see ApplicationDbContext's index.
        [Required(ErrorMessage = "Rating name is required.")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "Rating name must be between {2} and {1} characters.")]
        [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Rating name can only contain letters, numbers, and hyphens (e.g. PG-13).")]
        [Display(Name = "Rating")]
        public string Name { get; set; } = string.Empty;

        // Short meaning of the rating (e.g. "Parents Strongly Cautioned").
        [StringLength(200, ErrorMessage = "Description can be at most {1} characters.")]
        public string? Description { get; set; }

        // Movies with this audience rating.
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
