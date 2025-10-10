using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Repositories;

/// <summary>
/// Repository interface for tenant webhook operations.
/// </summary>
public interface ITenantWebhookRepository
{
    /// <summary>
    /// Gets a webhook by ID.
    /// </summary>
    Task<TenantWebhook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all webhooks for a tenant.
    /// </summary>
    Task<IEnumerable<TenantWebhook>> GetByTenantIdAsync(Guid tenantId, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active webhooks subscribed to a specific event.
    /// </summary>
    Task<IEnumerable<TenantWebhook>> GetActiveForEventAsync(Guid tenantId, TenantWebhookEventType eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    Task<TenantWebhook> CreateAsync(TenantWebhook webhook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing webhook.
    /// </summary>
    Task<TenantWebhook> UpdateAsync(TenantWebhook webhook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a webhook (soft delete).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple webhooks in bulk.
    /// </summary>
    Task<IEnumerable<TenantWebhook>> BulkCreateAsync(IEnumerable<TenantWebhook> webhooks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets webhook deliveries with optional filters.
    /// </summary>
    Task<(IEnumerable<TenantWebhookDelivery> Deliveries, int TotalCount)> GetDeliveriesAsync(
        Guid webhookId,
        bool? success = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed webhook deliveries for retry.
    /// </summary>
    Task<(IEnumerable<TenantWebhookDelivery> Deliveries, int TotalCount)> GetFailedDeliveriesAsync(
        Guid tenantId,
        DateTime? sinceDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a webhook delivery attempt.
    /// </summary>
    Task<TenantWebhookDelivery> RecordDeliveryAsync(TenantWebhookDelivery delivery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a delivery by ID.
    /// </summary>
    Task<TenantWebhookDelivery?> GetDeliveryByIdAsync(Guid deliveryId, CancellationToken cancellationToken = default);
}
