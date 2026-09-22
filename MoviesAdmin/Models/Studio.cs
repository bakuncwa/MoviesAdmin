using System.ComponentModel.DataAnnotations;

namespace MoviesAdmin.Models
{
    public class Studio
    {
        // Primary key.
        public int Id { get; set; }

        // Studio/distributor name (e.g. "A24", "20th Century Studios"); unique, see the index in
        // ApplicationDbContext.
        [Required]
        [StringLength(150)]
        [RegularExpression(@"^[\p{L}\p{N}\s\-&.,']+$", ErrorMessage = "Studio name can only contain letters, numbers, spaces, and common punctuation (-&.,').")]
        public string Name { get; set; } = string.Empty;

        // One production studio/distributor can be credited on many movies; a movie belongs to at
        // most one studio (see Movie.StudioId) — one-to-many, per README's future-models roadmap.
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
