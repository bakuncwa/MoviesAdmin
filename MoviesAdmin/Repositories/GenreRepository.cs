using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public class GenreRepository : Repository<Genre>, IGenreRepository
    {
        public GenreRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<Genre?> GetByNameAsync(string name) =>
            Set.FirstOrDefaultAsync(g => g.Name == name);
    }
}
