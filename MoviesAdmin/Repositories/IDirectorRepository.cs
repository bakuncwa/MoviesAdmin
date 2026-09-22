using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public interface IDirectorRepository : IRepository<Director>
    {
        Task<Director?> GetByNameAsync(string name);
    }
}
