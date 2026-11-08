using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    // Every query takes the critic's user id (Review.CreatedBy), so ReviewsController can only
    // ever list, load, edit or delete the signed-in critic's own reviews.
    public interface IReviewRepository : IRepository<Review>
    {
        // My Reviews page: everything one critic has written, newest first, with the movie loaded.
        Task<IEnumerable<Review>> GetByCriticAsync(string createdBy);

        // One review, only if it belongs to this critic; null otherwise (including someone else's).
        Task<Review?> GetByIdForCriticAsync(int id, string createdBy);

        // The critic's review of this movie, if they've written one (one review per movie).
        Task<Review?> GetByMovieForCriticAsync(int movieId, string createdBy);
    }
}
