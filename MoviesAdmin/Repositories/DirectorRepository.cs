using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public class DirectorRepository : Repository<Director>, IDirectorRepository
    {
        public DirectorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<Director?> GetByNameAsync(string name) =>
            Set.FirstOrDefaultAsync(d => d.Name == name);
    }
}
