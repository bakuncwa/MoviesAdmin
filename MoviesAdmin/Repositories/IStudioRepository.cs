using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public interface IStudioRepository : IRepository<Studio>
    {
        Task<Studio?> GetByNameAsync(string name);
    }
}
