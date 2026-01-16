using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Repository implementation for Order entities
/// </summary>
public class OrderRepository(IApplicationDbContext context) 
    : CommerceRepositoryBase<Order>(context), IOrderRepository
{
    /// <inheritdoc />
    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetByUserIdAsync(
        Guid userId,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Include(o => o.LineItems)
            .Where(o => o.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetByTenantIdAsync(
        Guid tenantId,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Include(o => o.LineItems)
            .Where(o => o.TenantId == tenantId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Include(o => o.LineItems)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order?> GetWithLineItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(order, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public new Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        Entities.Update(order);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {
        order.SoftDelete();
        Entities.Update(order);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
