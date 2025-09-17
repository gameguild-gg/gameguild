using System.Linq.Expressions;


namespace GameGuild;

/// <summary> Generic repository interface for domain entities </summary>
/// <typeparam name="T"> The entity type </typeparam>
/// <typeparam name="TKey"> The key type </typeparam>
public interface IRepository<T, in TKey> where T : class, IEntity<TKey> where TKey : IEquatable<TKey> {
  /// <summary> Gets an entity by its identifier </summary>
  Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

  /// <summary> Gets all entities </summary>
  Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

  /// <summary> Gets entities with pagination </summary>
  Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

  /// <summary> Finds entities matching the specified predicate </summary>
  Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

  /// <summary> Gets the first entity matching the predicate or null </summary>
  Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

  /// <summary> Checks if any entity matches the predicate </summary>
  Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

  /// <summary> Counts entities matching the predicate </summary>
  Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

  /// <summary> Adds a new entity </summary>
  Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary> Adds multiple entities </summary>
  Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

  /// <summary> Updates an existing entity </summary>
  Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary> Updates multiple entities </summary>
  Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

  /// <summary> Removes an entity </summary>
  Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary> Removes an entity by its identifier </summary>
  Task RemoveAsync(TKey id, CancellationToken cancellationToken = default);

  /// <summary> Removes multiple entities </summary>
  Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

  /// <summary> Soft deletes an entity by its identifier </summary>
  Task SoftDeleteAsync(TKey id, CancellationToken cancellationToken = default);

  /// <summary> Restores a soft-deleted entity by its identifier </summary>
  Task RestoreAsync(TKey id, CancellationToken cancellationToken = default);

  /// <summary> Saves changes to the repository </summary>
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary> Repository interface for entities with Guid keys </summary>
/// <typeparam name="T"> The entity type </typeparam>
public interface IRepository<T> : IRepository<T, Guid> where T : class, IEntity<Guid> { }
