using MoviesAdmin.Models;
using MoviesAdmin.Services.ReviewText;

namespace MoviesAdmin.ViewModels.Movies
{
    // GET Movies/Reviews/5: one movie's published reviews, shown as rows that expand to the full text.
    public class MovieReviewsViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public string PosterUrl { get; set; } = string.Empty;
        public double? AverageRating { get; set; }
        public List<MovieReviewRow> Reviews { get; set; } = new();

        public static MovieReviewsViewModel From(Movie movie, IEnumerable<(Review Review, string CriticName)> reviews) => new()
        {
            MovieId = movie.Id,
            Title = movie.Title,
            ReleaseYear = movie.ReleaseDate.Year,
            PosterUrl = MoviePosterResolver.Resolve(movie),
            AverageRating = movie.AverageRating,
            Reviews = reviews.Select(x => MovieReviewRow.From(x.Review, x.CriticName)).ToList()
        };
    }

    // One collapsible row: the summary line (critic, stars, date, excerpt) and the full review.
    public class MovieReviewRow
    {
        private const int ExcerptLength = 140;

        public int Id { get; set; }
        public string CriticName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public DateTime? PublishedAt { get; set; }

        // Plain-text start of the review for the collapsed row.
        public string Excerpt { get; set; } = string.Empty;

        // The full review, already sanitized for Html.Raw.
        public string Html { get; set; } = string.Empty;

        // First letter of the critic's name, for the round badge.
        public string Initial => string.IsNullOrWhiteSpace(CriticName) ? "?" : CriticName.Trim()[..1].ToUpperInvariant();

        public static MovieReviewRow From(Review review, string criticName)
        {
            var plain = ReviewHtml.PlainText(review.Text).Trim();
            return new MovieReviewRow
            {
                Id = review.Id,
                CriticName = criticName,
                Rating = review.Rating,
                PublishedAt = review.PublishedAt,
                Excerpt = plain.Length <= ExcerptLength ? plain : plain[..ExcerptLength].TrimEnd() + "…",
                Html = ReviewHtml.Sanitize(review.Text)
            };
        }
    }
}
