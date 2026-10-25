using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoviesAdmin.Models
{
    public class Movie
    {
        // Primary key.
        public int Id { get; set; }

        // Movie's display title, shown everywhere (table rows, modals, search results).
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between {2} and {1} characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s\-':,.&!?()]+$", ErrorMessage = "Title can only contain letters, numbers, spaces, and common punctuation (-':,.&!?()).")]
        public string Title { get; set; } = string.Empty;

        // Optional plot summary/blurb shown on the Details view.
        [StringLength(2000, ErrorMessage = "Synopsis can be at most {1} characters.")]
        [RegularExpression(@"^[^<>]*$", ErrorMessage = "Synopsis cannot contain '<' or '>' characters.")]
        public string? Synopsis { get; set; }

        // Theatrical/original release date; also used for year-based search/filter and sorting.
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        // Runtime in minutes; nullable since it may not be known when a movie is first added.
        // The form collects it as hours + minutes and the UI shows it as e.g. "2h 9m".
        [Range(1, 960, ErrorMessage = "Runtime must be between {1} and {2} minutes.")]
        public int? RuntimeMinutes { get; set; }

        // One-to-many: a movie belongs to one genre (see Genre.cs).
        // Foreign key to this movie's genre.
        [Range(1, int.MaxValue, ErrorMessage = "Choose a genre.")]
        [Display(Name = "Genre")]
        public int GenreId { get; set; }
        // Navigation to the genre entity itself.
        public Genre? Genre { get; set; }

        // One-to-many: a movie belongs to one audience rating such as PG-13 (see AudienceRating.cs).
        // Foreign key to this movie's audience rating.
        [Range(1, int.MaxValue, ErrorMessage = "Choose an audience rating.")]
        [Display(Name = "Audience Rating")]
        public int AudienceRatingId { get; set; }
        // Navigation to the audience rating entity itself.
        public AudienceRating? AudienceRating { get; set; }

        // Poster image sourced from an external movie API/site, stored as a plain link rather
        // than downloaded. Distinct from PosterImagePath (see below); a movie may have either,
        // both, or neither, with PosterImagePath taking display priority when both are set.
        [StringLength(500, ErrorMessage = "Poster URL can be at most {1} characters.")]
        [RegularExpression(@"^https?://\S+$", ErrorMessage = "Poster URL must be a valid http:// or https:// address.")]
        public string? PosterUrl { get; set; }

        // Poster image uploaded through the admin UI: its Azure Blob Storage URL (e.g.
        // "https://<account>.blob.core.windows.net/posters/abc.jpg", or with Azurite in Docker
        // "http://127.0.0.1:10000/devstoreaccount1/posters/abc.jpg"), or a relative path under wwwroot
        // (e.g. "images/movies/inception.jpg") when blob storage isn't configured. Distinct from PosterUrl, which is reserved for posters sourced from an
        // external movie API. Views should fall back to PosterUrl when this is null.
        [StringLength(500, ErrorMessage = "Poster image path can be at most {1} characters.")]
        [RegularExpression(@"^(https?://\S+|[\w\-./]+)\.(?i:jpg|jpeg|png|gif|webp)$", ErrorMessage = "Poster image path must be an http:// or https:// URL or a relative path ending in .jpg, .jpeg, .png, .gif, or .webp.")]
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

        // User-submitted reviews/ratings for this movie.
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Rotten-Tomatoes-style aggregate; derived from Reviews rather than stored, so it can never drift.
        [NotMapped]
        public double? AverageRating => Reviews.Count == 0 ? null : Reviews.Average(r => r.Rating);
    }
}
