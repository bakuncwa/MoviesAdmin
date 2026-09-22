using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace MoviesAdmin.Models
{
    // One trailer per movie (one-to-one, enforced by the unique index on MovieId in
    // ApplicationDbContext), mirroring how PosterUrl/PosterImagePath hold a single poster.
    // Only the YouTube URL itself is persisted; the embeddable <iframe src> is derived from it on
    // read (see EmbedUrl) rather than storing raw HTML, so nothing user-supplied is ever written
    // straight into the page.
    public class Trailer
    {
        // Primary key.
        public int Id { get; set; }

        // FK to the owning movie; unique (see ApplicationDbContext), enforcing one trailer per movie.
        public int MovieId { get; set; }
        // Navigation back to the movie itself.
        public Movie Movie { get; set; } = null!;

        // The trailer's YouTube link exactly as entered (watch?v=, youtu.be/, or embed/ form).
        [Required]
        [StringLength(500)]
        [RegularExpression(
            @"^https?://(www\.)?(youtube\.com/(watch\?v=|embed/)|youtu\.be/)[A-Za-z0-9_\-]{6,20}([?&]\S*)?$",
            ErrorMessage = "Trailer URL must be a youtube.com or youtu.be link.")]
        public string YouTubeUrl { get; set; } = string.Empty;

        // When this trailer link was added/last set; defaults to now at creation time.
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Parsed from YouTubeUrl on demand rather than stored, so it always reflects whatever
        // URL currently lives in the column, no matter which of the accepted URL shapes it's in.
        [NotMapped]
        public string? YouTubeVideoId => YouTubeTrailerHelper.ExtractVideoId(YouTubeUrl);

        // youtube-nocookie.com is YouTube's own privacy-enhanced embed domain — same player,
        // no personalized tracking cookies until the viewer actually presses play.
        [NotMapped]
        public string? EmbedUrl => YouTubeVideoId is null
            ? null
            : $"https://www.youtube-nocookie.com/embed/{YouTubeVideoId}";
    }

    // Shared id-extraction logic for the URL shapes RegularExpression above accepts:
    // watch?v=ID, youtu.be/ID, and embed/ID (each optionally followed by &/?-prefixed params).
    public static class YouTubeTrailerHelper
    {
        private static readonly Regex VideoIdPattern = new(
            @"(?:youtube\.com/(?:watch\?v=|embed/)|youtu\.be/)(?<id>[A-Za-z0-9_\-]{6,20})",
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

        public static string? ExtractVideoId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var match = VideoIdPattern.Match(url);
            return match.Success ? match.Groups["id"].Value : null;
        }
    }
}
