using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public interface IMovieRepository : IRepository<Movie>
    {
        // Detail page: one movie with its genres and reviews (+ reviewer) eager-loaded.
        Task<Movie?> GetByIdWithDetailsAsync(int id);

        // Browse page: search/filter/sort per the "Enhancing" feature set. Every filter is optional
        // and composed onto one IQueryable, so SQL Server does the filtering and ordering.
        Task<IEnumerable<Movie>> SearchAsync(string? titleQuery, int? genreId, int? releaseYear, ContentRating? contentRating, MovieSortOrder sortOrder);

        // Distinct release years in the catalog, newest first, for the year filter dropdown.
        Task<List<int>> GetReleaseYearsAsync();
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
