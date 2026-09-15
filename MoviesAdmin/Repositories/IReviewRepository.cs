using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public interface IReviewRepository : IRepository<Review>
    {
        // Movie detail page: this movie's reviews.
        Task<IEnumerable<Review>> GetByMovieIdAsync(int movieId);

        // Admin moderation queue / user profile: everything one user has submitted.
        Task<IEnumerable<Review>> GetByUserIdAsync(string userId);
    }
}
