using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    public class Genre
    {
        // Primary key.
        public int Id { get; set; }

        // Genre label (e.g. "Sci-Fi", "Action & Adventure"); unique, see ApplicationDbContext's index.
        [Required]
        [StringLength(50)]
        [RegularExpression(@"^[\p{L}\p{N}\s\-&]+$", ErrorMessage = "Genre name can only contain letters, numbers, spaces, hyphens, and ampersands.")]
        public string Name { get; set; } = string.Empty;

        // Movies assigned to this genre (many-to-many via the MovieGenre join entity).
        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    }
}
