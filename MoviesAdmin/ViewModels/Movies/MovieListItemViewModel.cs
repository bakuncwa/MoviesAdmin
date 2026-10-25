using System.Text.RegularExpressions;
using MoviesAdmin.Models;

namespace MoviesAdmin.ViewModels.Movies
{
    // One row of the admin Movies table (Views/Movies/Partials/_Index.cshtml), plus the extra
    // fields the View/Edit/Delete modals need without a second round trip (see the data-* attrs
    // rendered onto each row's action buttons).
    public class MovieListItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Synopsis { get; set; }

        public DateTime ReleaseDate { get; set; }

        public int? RuntimeMinutes { get; set; }

        // "2h 9m" style runtime, or "—" when unknown.
        public string RuntimeText => MovieDisplayFormat.Runtime(RuntimeMinutes);

        // "PG-13" style audience rating, or "Not Rated".
        public string AudienceRatingName { get; set; } = string.Empty;

        // Resolved once here so views never have to choose between PosterImagePath/PosterUrl themselves.
        public string DisplayImageUrl { get; set; } = string.Empty;

        public string GenreName { get; set; } = string.Empty;

        // URL form of GenreName for the /Movies/{genre} link (e.g. "action-adventure").
        public string GenreSlug => MovieDisplayFormat.Slug(GenreName);

        public double? AverageRating { get; set; }

        public int ReviewCount { get; set; }

        public static MovieListItemViewModel FromEntity(Movie movie)
        {
            return new MovieListItemViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Synopsis = movie.Synopsis,
                ReleaseDate = movie.ReleaseDate,
                RuntimeMinutes = movie.RuntimeMinutes,
                AudienceRatingName = movie.AudienceRating?.Name ?? "Not Rated",
                DisplayImageUrl = MoviePosterResolver.Resolve(movie),
                GenreName = movie.Genre?.Name ?? string.Empty,
                AverageRating = movie.Reviews.Count == 0 ? null : movie.Reviews.Average(r => r.Rating),
                ReviewCount = movie.Reviews.Count
            };
        }
    }

    // Shared display formatting for the Movies views.
    public static class MovieDisplayFormat
    {
        // 129 -> "2h 9m", 120 -> "2h", 45 -> "45m", null -> "—".
        public static string Runtime(int? totalMinutes)
        {
            if (totalMinutes is not > 0)
            {
                return "—";
            }

            var hours = totalMinutes.Value / 60;
            var minutes = totalMinutes.Value % 60;
            if (hours == 0) return $"{minutes}m";
            return minutes == 0 ? $"{hours}h" : $"{hours}h {minutes}m";
        }

        // "Action & Adventure" -> "action-adventure", "Sci-Fi" -> "sci-fi". The genre segment of
        // /Movies/{genre}; MoviesController.Index maps it back to the Genre by comparing slugs.
        public static string Slug(string name) =>
            Regex.Replace(name.ToLowerInvariant(), @"[^\p{L}\p{N}]+", "-").Trim('-');
    }

    // Shared by every Movies view model so PosterImagePath (uploaded file) vs. PosterUrl
    // (external API) vs. "no poster at all" is resolved exactly the same way everywhere.
    public static class MoviePosterResolver
    {
        public const string PlaceholderImageUrl = "/images/movies/placeholder.svg";

        public static string Resolve(Movie movie)
        {
            if (!string.IsNullOrWhiteSpace(movie.PosterImagePath))
            {
                return UploadedPosterUrl(movie.PosterImagePath);
            }

            if (!string.IsNullOrWhiteSpace(movie.PosterUrl))
            {
                return movie.PosterUrl;
            }

            return PlaceholderImageUrl;
        }

        // PosterImagePath is a blob URL (Azure Blob Storage, used as-is) or a path under wwwroot.
        public static string UploadedPosterUrl(string posterImagePath) =>
            Uri.IsWellFormedUriString(posterImagePath, UriKind.Absolute)
                ? posterImagePath
                : "/" + posterImagePath.TrimStart('/');
    }
}
