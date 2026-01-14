using System.Linq.Expressions;
using GameGuild.Abstractions;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce;

/// <summary>
///     Base repository with common query patterns for Commerce entities.
///     Provides standard soft-delete filtering, ordering, and query methods.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public abstract class CommerceRepositoryBase<TEntity>
    where TEntity : EntityBase
{
    protected readonly IApplicationDbContext Context;

    protected CommerceRepositoryBase(IApplicationDbContext context)
    {
        Context = context;
    }

    /// <summary>
    ///     Gets the DbSet for the entity type.
    /// </summary>
    protected virtual DbSet<TEntity> Entities => Context.Set<TEntity>();

    /// <summary>
    ///     Gets a queryable with standard filters applied (soft-delete excluded).
    /// </summary>
    protected virtual IQueryable<TEntity> Query => Entities.Where(e => e.DeletedAt == null);

    /// <summary>
    ///     Gets a queryable ordered by creation date (most recent first).
    /// </summary>
    protected virtual IQueryable<TEntity> QueryOrdered => Query.OrderByDescending(e => e.CreatedAt);

    /// <summary>
    ///     Gets entity by ID with standard filters.
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity or null if not found</returns>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets all entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching entities</returns>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = QueryOrdered;

        if (predicate != null)
            query = query.Where(predicate);

        return await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets paged results with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip</param>
    /// <param name="take">Number of items to take</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged collection of entities</returns>
    public virtual async Task<IEnumerable<TEntity>> GetPagedAsync(
        int skip,
        int take,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = QueryOrdered;

        if (predicate != null)
            query = query.Where(predicate);

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Counts entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of matching entities</returns>
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query;

        if (predicate != null)
            query = query.Where(predicate);

        return await query
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Checks if any entity matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches, false otherwise</returns>
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(predicate, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates a new entity.
    /// </summary>
    /// <param name="entity">The entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created entity</returns>
    public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    /// <summary>
    ///     Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated entity</returns>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        Entities.Update(entity);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    /// <summary>
    ///     Soft-deletes an entity.
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return false;

        entity.SoftDelete();
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Hard-deletes an entity (use with caution).
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    public virtual async Task<bool> HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Entities
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity == null)
            return false;

        Entities.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Gets entities by tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of entities for the tenant</returns>
    public virtual async Task<IEnumerable<TEntity>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await QueryOrdered
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
