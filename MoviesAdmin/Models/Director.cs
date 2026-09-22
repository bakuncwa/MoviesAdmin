using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    public class Director
    {
        // Primary key.
        public int Id { get; set; }

        // Director's full name; unique, see ApplicationDbContext's index.
        [Required]
        [StringLength(150)]
        [RegularExpression(@"^[\p{L}\p{N}\s\-'.]+$", ErrorMessage = "Director name can only contain letters, numbers, spaces, hyphens, apostrophes, and periods.")]
        public string Name { get; set; } = string.Empty;

        // Optional short biography.
        [StringLength(2000)]
        [RegularExpression(@"^[^<>]*$", ErrorMessage = "Bio cannot contain '<' or '>' characters.")]
        public string? Bio { get; set; }

        // One director can be credited on many movies; a movie has at most one primary director
        // (see Movie.DirectorId) — this is intentionally one-to-many rather than many-to-many,
        // per the "Suggested Future Data Models" note in README.md.
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
