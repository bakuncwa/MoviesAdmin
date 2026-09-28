using MoviesAdmin.Models;

namespace MoviesAdmin.ViewModels.Movies
{
    // Backs the View modal (_MovieDetailsModal.cshtml): poster on the left, details on the right.
    public class MovieDetailsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Synopsis { get; set; }

        public DateTime ReleaseDate { get; set; }

        public int? RuntimeMinutes { get; set; }

        public string RuntimeText => MovieDisplayFormat.Runtime(RuntimeMinutes);

        public string ContentRatingLabel { get; set; } = string.Empty;

        public string DisplayImageUrl { get; set; } = string.Empty;

        public List<string> GenreNames { get; set; } = new();

        public double? AverageRating { get; set; }

        public int ReviewCount { get; set; }

        public string? DirectorName { get; set; }

        public string? StudioName { get; set; }

        // The modal only shows a "Watch Trailer" button when this is true; the actual <iframe>
        // is lazily fetched from GET /Movies/TrailerEmbed/{id} when that button is clicked,
        // rather than being embedded up front for every movie the admin opens.
        public bool HasTrailer { get; set; }

        public static MovieDetailsViewModel FromEntity(Movie movie)
        {
            return new MovieDetailsViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Synopsis = movie.Synopsis,
                ReleaseDate = movie.ReleaseDate,
                RuntimeMinutes = movie.RuntimeMinutes,
                ContentRatingLabel = movie.ContentRating.ToLabel(),
                DisplayImageUrl = MoviePosterResolver.Resolve(movie),
                GenreNames = movie.MovieGenres
                    .Select(mg => mg.Genre.Name)
                    .OrderBy(name => name)
                    .ToList(),
                AverageRating = movie.Reviews.Count == 0 ? null : movie.Reviews.Average(r => r.Rating),
                ReviewCount = movie.Reviews.Count,
                DirectorName = movie.Director?.Name,
                StudioName = movie.Studio?.Name,
                HasTrailer = movie.Trailer?.EmbedUrl != null
            };
        }
    }
}
