using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public interface IMovieRepository : IRepository<Movie>
    {
        // Detail page: one movie with its genre, audience rating and reviews (+ reviewer) eager-loaded.
        Task<Movie?> GetByIdWithDetailsAsync(int id);

        // Browse page: search/filter/sort per the "Enhancing" feature set. Every filter is optional
        // and composed onto one IQueryable, so SQL Server does the filtering and ordering.
        Task<IEnumerable<Movie>> SearchAsync(string? titleQuery, int? genreId, int? releaseYear, int? audienceRatingId, MovieSortOrder sortOrder);

        // Distinct release years in the catalog, newest first, for the year filter dropdown.
        Task<List<int>> GetReleaseYearsAsync();

        // A movie's published reviews (drafts excluded), newest first, each with its critic's name
        // from the UserSnapshot table (falling back to the name saved on the review).
        Task<List<(Review Review, string CriticName)>> GetPublishedReviewsAsync(int movieId);
    }

    // Bound by name from the query string (?sort=TitleAsc), so renaming a member breaks saved links.
    public enum MovieSortOrder
    {
        TitleAsc,
        ReleaseDateDesc,
        RatingDesc,
        ReleaseDateAsc,
        TitleDesc,
        RuntimeDesc,
        RuntimeAsc
    }
}
