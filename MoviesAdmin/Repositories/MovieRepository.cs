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
            return await Set
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Movie>> SearchAsync(string? titleQuery, int? genreId, int? releaseYear, MovieSortOrder sortOrder)
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

            query = sortOrder switch
            {
                MovieSortOrder.ReleaseDateDesc => query.OrderByDescending(m => m.ReleaseDate),
                MovieSortOrder.RatingDesc => query.OrderByDescending(m => m.Reviews.Average(r => (double?)r.Rating) ?? 0),
                _ => query.OrderBy(m => m.Title)
            };

            return await query.ToListAsync();
        }
    }
}
