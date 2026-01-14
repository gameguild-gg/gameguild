using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Repository implementation for Order entities
/// </summary>
public class OrderRepository(IApplicationDbContext context) : IOrderRepository
{
    private DbSet<Order> Orders => context.Set<Order>();

    /// <inheritdoc />
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await Orders
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
        var query = Orders
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
        var query = Orders
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
        var query = Orders
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
        return await Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await Orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        Orders.Update(order);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {
        order.SoftDelete();
        Orders.Update(order);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
