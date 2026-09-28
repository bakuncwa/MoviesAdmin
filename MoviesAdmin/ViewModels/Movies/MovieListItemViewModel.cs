using MoviesAdmin.Models;

namespace MoviesAdmin.ViewModels.Movies
{
    // One row of the admin Movies table (Views/Movies/_MovieTableRows.cshtml), plus the extra
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
        public string ContentRatingLabel { get; set; } = string.Empty;

        // Resolved once here so views never have to choose between PosterImagePath/PosterUrl themselves.
        public string DisplayImageUrl { get; set; } = string.Empty;

        // Comma-joined for the table cell; Details/Edit modals re-fetch the real list from the server.
        public string GenreNames { get; set; } = string.Empty;

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
                ContentRatingLabel = movie.ContentRating.ToLabel(),
                DisplayImageUrl = MoviePosterResolver.Resolve(movie),
                GenreNames = string.Join(", ", movie.MovieGenres
                    .Select(mg => mg.Genre.Name)
                    .OrderBy(name => name)),
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
                return "/" + movie.PosterImagePath.TrimStart('/');
            }

            if (!string.IsNullOrWhiteSpace(movie.PosterUrl))
            {
                return movie.PosterUrl;
            }

            return PlaceholderImageUrl;
        }
    }
}
