using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoviesAdmin.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Synopsis { get; set; }

        public DateTime ReleaseDate { get; set; }

        public int? RuntimeMinutes { get; set; }

        [StringLength(500)]
        public string? PosterUrl { get; set; }

        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Rotten-Tomatoes-style aggregate; derived from Reviews rather than stored, so it can never drift.
        [NotMapped]
        public double? AverageRating => Reviews.Count == 0 ? null : Reviews.Average(r => r.Rating);
    }
}
