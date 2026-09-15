using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public interface IGenreRepository : IRepository<Genre>
    {
        Task<Genre?> GetByNameAsync(string name);
    }
}
