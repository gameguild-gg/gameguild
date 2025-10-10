using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Commands;

// Webhook management commands
public record RegisterTenantWebhookCommand(
    Guid TenantId,
    string Url,
    TenantWebhookEventType EventType,
    string? Secret = null,
    int RetryCount = 3,
    int TimeoutSeconds = 30,
    string? Headers = null
) : IRequest<TenantWebhook>;

public record UpdateTenantWebhookCommand(
    Guid WebhookId,
    string? Url = null,
    string? Secret = null,
    int? RetryCount = null,
    int? TimeoutSeconds = null,
    string? Headers = null
) : IRequest<TenantWebhook>;

public record DeleteTenantWebhookCommand(Guid WebhookId) : IRequest<bool>;

public record ActivateTenantWebhookCommand(Guid WebhookId) : IRequest<TenantWebhook>;

public record DeactivateTenantWebhookCommand(Guid WebhookId) : IRequest<TenantWebhook>;

public record TriggerTenantWebhookCommand(
    Guid TenantId,
    TenantWebhookEventType EventType,
    object Payload
) : IRequest<int>; // Returns number of webhooks triggered

// Webhook query commands
public record GetTenantWebhooksQuery(
    Guid TenantId,
    TenantWebhookEventType? EventType = null,
    bool? IsActive = null
) : IRequest<IEnumerable<TenantWebhook>>;

public record GetWebhookDeliveriesQuery(
    Guid WebhookId,
    WebhookDeliveryStatus? Status = null,
    int PageSize = 50,
    int PageNumber = 1
) : IRequest<IEnumerable<TenantWebhookDelivery>>;

public record GetWebhookStatisticsQuery(Guid WebhookId) : IRequest<WebhookStatistics>;

// DTOs
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
