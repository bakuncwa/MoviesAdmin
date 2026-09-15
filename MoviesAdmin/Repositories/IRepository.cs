namespace MoviesAdmin.Repositories
{
    // Generic contract every entity-specific repository (IMovieRepository, IGenreRepository,
    // IReviewRepository, ...) inherits from, so controllers only ever depend on abstractions.
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(int id);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        Task<int> SaveChangesAsync();
    }
}
