using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public interface IMovieRepository : IRepository<Movie>
    {
        // Detail page: one movie with its genres and reviews (+ reviewer) eager-loaded.
        Task<Movie?> GetByIdWithDetailsAsync(int id);

        // Browse page: search/filter/sort per the "Enhancing" feature set (genre, release year, rating, popularity).
        Task<IEnumerable<Movie>> SearchAsync(string? titleQuery, int? genreId, int? releaseYear, MovieSortOrder sortOrder);
    }

    public enum MovieSortOrder
    {
        TitleAsc,
        ReleaseDateDesc,
        RatingDesc
    }
}
