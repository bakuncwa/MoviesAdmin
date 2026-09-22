using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;

namespace MoviesAdmin.Repositories
{
    public class StudioRepository : Repository<Studio>, IStudioRepository
    {
        public StudioRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<Studio?> GetByNameAsync(string name) =>
            Set.FirstOrDefaultAsync(s => s.Name == name);
    }
}
