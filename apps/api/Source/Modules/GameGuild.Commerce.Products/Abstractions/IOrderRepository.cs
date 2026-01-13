namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository interface for Order entities
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Get order by ID
    /// </summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order by idempotency key (for duplicate prevention)
    /// </summary>
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get orders by user ID
    /// </summary>
    Task<IEnumerable<Order>> GetByUserIdAsync(
        Guid userId,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get orders by tenant ID
    /// </summary>
    Task<IEnumerable<Order>> GetByTenantIdAsync(
        Guid tenantId,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get orders within a date range
    /// </summary>
    Task<IEnumerable<Order>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order with line items included
    /// </summary>
    Task<Order?> GetWithLineItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new order
    /// </summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing order
    /// </summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete order (soft delete)
    /// </summary>
    Task DeleteAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
