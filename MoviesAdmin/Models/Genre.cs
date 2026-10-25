using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    public class Genre
    {
        // Primary key.
        public int Id { get; set; }

        // Genre label (e.g. "Sci-Fi", "Action & Adventure"); unique, see ApplicationDbContext's index.
        [Required(ErrorMessage = "Genre name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Genre name must be between {2} and {1} characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s\-&]+$", ErrorMessage = "Genre name can only contain letters, numbers, spaces, hyphens, and ampersands.")]
        [Display(Name = "Genre")]
        public string Name { get; set; } = string.Empty;

        // One-to-many: a genre can have many movies, and a movie belongs to one genre (Movie.GenreId).
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
