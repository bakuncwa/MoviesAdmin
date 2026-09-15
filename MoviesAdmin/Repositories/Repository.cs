using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;

namespace MoviesAdmin.Repositories
{
    // Shared EF Core implementation. Entity-specific repositories inherit this for the
    // common CRUD plumbing and add only the queries that are specific to that entity, e.g.:
    //
    //   public interface IMovieRepository : IRepository<Movie>
    //   {
    //       Task<IEnumerable<Movie>> GetByGenreAsync(int genreId);
    //   }
    //
    //   public class MovieRepository : Repository<Movie>, IMovieRepository
    //   {
    //       public MovieRepository(ApplicationDbContext context) : base(context) { }
    //
    //       public Task<IEnumerable<Movie>> GetByGenreAsync(int genreId) =>
    //           ... query Set.Where(m => m.GenreId == genreId) ...
    //   }
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly ApplicationDbContext Context;
        protected readonly DbSet<TEntity> Set;

        public Repository(ApplicationDbContext context)
        {
            Context = context;
            Set = context.Set<TEntity>();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync() => await Set.ToListAsync();

        public async Task<TEntity?> GetByIdAsync(int id) => await Set.FindAsync(id);

        public async Task AddAsync(TEntity entity) => await Set.AddAsync(entity);

        public void Update(TEntity entity) => Set.Update(entity);

        public void Remove(TEntity entity) => Set.Remove(entity);

        public Task<int> SaveChangesAsync() => Context.SaveChangesAsync();
    }
}
