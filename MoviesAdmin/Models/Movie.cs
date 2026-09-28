using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoviesAdmin.Models
{
    public class Movie
    {
        // Primary key.
        public int Id { get; set; }

        // Movie's display title, shown everywhere (table rows, modals, search results).
        [Required]
        [StringLength(200)]
        [RegularExpression(@"^[\p{L}\p{N}\s\-':,.&!?()]+$", ErrorMessage = "Title can only contain letters, numbers, spaces, and common punctuation (-':,.&!?()).")]
        public string Title { get; set; } = string.Empty;

        // Optional plot summary/blurb shown on the Details view.
        [StringLength(2000)]
        [RegularExpression(@"^[^<>]*$", ErrorMessage = "Synopsis cannot contain '<' or '>' characters.")]
        public string? Synopsis { get; set; }

        // Theatrical/original release date; also used for year-based search/filter and sorting.
        public DateTime ReleaseDate { get; set; }

        // Runtime in minutes; nullable since it may not be known when a movie is first added.
        // The form collects it as hours + minutes and the UI shows it as e.g. "2h 9m".
        public int? RuntimeMinutes { get; set; }

        // Audience rating such as PG-13 (see ContentRating.cs). Nullable for movies added before
        // the field existed; the Create/Edit form requires it.
        public ContentRating? ContentRating { get; set; }

        // Poster image sourced from an external movie API/site, stored as a plain link rather
        // than downloaded. Distinct from PosterImagePath (see below); a movie may have either,
        // both, or neither, with PosterImagePath taking display priority when both are set.
        [StringLength(500)]
        [RegularExpression(@"^https?://\S+$", ErrorMessage = "Poster URL must be a valid http:// or https:// address.")]
        public string? PosterUrl { get; set; }

        // Relative path (under wwwroot, e.g. "images/movies/inception.jpg") to a poster image uploaded
        // through the admin UI. Distinct from PosterUrl, which is reserved for posters sourced from an
        // external movie API. Views should fall back to PosterUrl when this is null.
        [StringLength(500)]
        [RegularExpression(@"^[\w\-./]+\.(?i:jpg|jpeg|png|gif|webp)$", ErrorMessage = "Poster image path must be a relative path ending in .jpg, .jpeg, .png, .gif, or .webp.")]
        public string? PosterImagePath { get; set; }

        // Optional: not every movie has a director/studio recorded yet, so both FKs are nullable
        // rather than forcing every Create/Edit to supply one.
        // Foreign key to this movie's primary director.
        public int? DirectorId { get; set; }
        // Navigation to the director entity itself (see Director.cs).
        public Director? Director { get; set; }

        // Foreign key to the studio/distributor that produced this movie.
        public int? StudioId { get; set; }
        // Navigation to the studio entity itself (see Studio.cs).
        public Studio? Studio { get; set; }

        // One-to-one; see Trailer.cs. Nullable because not every movie has a trailer recorded.
        public Trailer? Trailer { get; set; }

        // This movie's genre assignments (many-to-many via the MovieGenre join entity).
        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();

        // User-submitted reviews/ratings for this movie.
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Rotten-Tomatoes-style aggregate; derived from Reviews rather than stored, so it can never drift.
        [NotMapped]
        public double? AverageRating => Reviews.Count == 0 ? null : Reviews.Average(r => r.Rating);
    }
}
