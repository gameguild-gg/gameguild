using System.Linq.Expressions;


namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary> Generic repository interface for Testing Lab entities </summary>
/// <typeparam name="TEntity"> The entity type </typeparam>
/// <typeparam name="TKey"> The key type </typeparam>
public interface ITestingLabRepository<TEntity, in TKey> where TEntity : class {
  // Basic CRUD operations
  Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

  Task<TEntity?> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes);

  Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

  Task<IEnumerable<TEntity>> GetAsync(
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    params Expression<Func<TEntity, object>>[] includes
  );

  // Pagination
  Task<IEnumerable<TEntity>> GetWithPaginationAsync(
    int skip = 0,
    int take = 50,
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    CancellationToken cancellationToken = default
  );

  // Existence and counting
  Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

  Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

  Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

  // Modification operations
  Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

  Task<IEnumerable<TEntity>> CreateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

  Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

  Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

  Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

  Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

  // Transaction support
  Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);

  Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
