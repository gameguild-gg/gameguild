using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Services;

/// <summary>
/// Service for managing tenant lifecycle webhooks
/// </summary>
public interface ITenantWebhookService
{
    Task<TenantWebhook> RegisterWebhookAsync(
        Guid tenantId,
        string url,
        TenantWebhookEventType eventType,
        string? secret = null,
        int retryCount = 3,
        int timeoutSeconds = 30,
        string? headers = null,
        CancellationToken cancellationToken = default);

    Task<TenantWebhook> UpdateWebhookAsync(
        Guid webhookId,
        string? url = null,
        string? secret = null,
        int? retryCount = null,
        int? timeoutSeconds = null,
        string? headers = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);

    Task<TenantWebhook> ActivateWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);

    Task<TenantWebhook> DeactivateWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);

    Task<int> TriggerWebhooksAsync(
        Guid tenantId,
        TenantWebhookEventType eventType,
        object payload,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TenantWebhook>> GetTenantWebhooksAsync(
        Guid tenantId,
        TenantWebhookEventType? eventType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TenantWebhookDelivery>> GetWebhookDeliveriesAsync(
        Guid webhookId,
        WebhookDeliveryStatus? status = null,
        int pageSize = 50,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    Task<WebhookStatistics> GetWebhookStatisticsAsync(Guid webhookId, CancellationToken cancellationToken = default);
}

public record WebhookStatistics(
    Guid WebhookId,
    int TotalDeliveries,
    int SuccessfulDeliveries,
    int FailedDeliveries,
    int PendingDeliveries,
    double SuccessRate,
    DateTime? LastTriggeredAt,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt
);
