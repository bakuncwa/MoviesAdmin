using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public class MovieRepository : Repository<Movie>, IMovieRepository
    {
        public MovieRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Movie?> GetByIdWithDetailsAsync(int id)
        {
            // Used by Edit/Details/Delete, so this pulls in everything a single-movie view needs.
            // SearchAsync below stays leaner (no Director/Studio/Trailer) since the admin table
            // doesn't display them — see MovieListItemViewModel.
            return await Set
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews).ThenInclude(r => r.User)
                .Include(m => m.Director)
                .Include(m => m.Studio)
                .Include(m => m.Trailer)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Movie>> SearchAsync(string? titleQuery, int? genreId, int? releaseYear, ContentRating? contentRating, MovieSortOrder sortOrder)
        {
            var query = Set
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(titleQuery))
            {
                query = query.Where(m => m.Title.Contains(titleQuery));
            }

            if (genreId.HasValue)
            {
                query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));
            }

            if (releaseYear.HasValue)
            {
                query = query.Where(m => m.ReleaseDate.Year == releaseYear.Value);
            }

            if (contentRating.HasValue)
            {
                query = query.Where(m => m.ContentRating == contentRating.Value);
            }

            // Title is the tie-breaker everywhere so equal keys always come back in the same order.
            query = sortOrder switch
            {
                MovieSortOrder.ReleaseDateDesc => query.OrderByDescending(m => m.ReleaseDate).ThenBy(m => m.Title),
                MovieSortOrder.ReleaseDateAsc => query.OrderBy(m => m.ReleaseDate).ThenBy(m => m.Title),
                MovieSortOrder.TitleDesc => query.OrderByDescending(m => m.Title),
                MovieSortOrder.RatingDesc => query.OrderByDescending(m => m.Reviews.Average(r => (double?)r.Rating) ?? 0).ThenBy(m => m.Title),
                // Unknown runtimes sort last in both directions.
                MovieSortOrder.RuntimeDesc => query.OrderBy(m => m.RuntimeMinutes == null).ThenByDescending(m => m.RuntimeMinutes).ThenBy(m => m.Title),
                MovieSortOrder.RuntimeAsc => query.OrderBy(m => m.RuntimeMinutes == null).ThenBy(m => m.RuntimeMinutes).ThenBy(m => m.Title),
                _ => query.OrderBy(m => m.Title)
            };

            return await query.ToListAsync();
        }

        public Task<List<int>> GetReleaseYearsAsync() =>
            Set.Select(m => m.ReleaseDate.Year)
                .Distinct()
                .OrderByDescending(year => year)
                .ToListAsync();
    }
}
