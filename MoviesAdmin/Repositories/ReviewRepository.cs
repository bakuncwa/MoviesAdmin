using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetByCriticAsync(string createdBy)
        {
            return await Set
                .Include(r => r.Movie)
                .Where(r => r.CreatedBy == createdBy)
                .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetByIdForCriticAsync(int id, string createdBy)
        {
            return await Set
                .Include(r => r.Movie)
                .FirstOrDefaultAsync(r => r.Id == id && r.CreatedBy == createdBy);
        }

        public async Task<Review?> GetByMovieForCriticAsync(int movieId, string createdBy)
        {
            return await Set.FirstOrDefaultAsync(r => r.MovieId == movieId && r.CreatedBy == createdBy);
        }
    }
}
