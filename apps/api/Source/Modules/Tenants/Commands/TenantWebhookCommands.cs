using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

// Webhook management commands
public record RegisterTenantWebhookCommand(
    Guid TenantId,
    string Url,
    TenantWebhookEventType EventType,
    string? Secret = null,
    int RetryCount = 3,
    int TimeoutSeconds = 30,
    string? Headers = null
) : IRequest<Result<TenantWebhook>>;

public record UpdateTenantWebhookCommand(
    Guid WebhookId,
    string? Url = null,
    string? Secret = null,
    int? RetryCount = null,
    int? TimeoutSeconds = null,
    string? Headers = null
) : IRequest<Result<TenantWebhook>>;

public record DeleteTenantWebhookCommand(Guid WebhookId) : IRequest<Result<bool>>;

public record ActivateTenantWebhookCommand(Guid WebhookId) : IRequest<Result<TenantWebhook>>;

public record DeactivateTenantWebhookCommand(Guid WebhookId) : IRequest<Result<TenantWebhook>>;

public record TriggerTenantWebhookCommand(
    Guid TenantId,
    TenantWebhookEventType EventType,
    object Payload
) : IRequest<Result<int>>; // Returns number of webhooks triggered

// Webhook query commands
public record GetTenantWebhooksQuery(
    Guid TenantId,
    TenantWebhookEventType? EventType = null,
    bool? IsActive = null
) : IRequest<Result<IEnumerable<TenantWebhook>>>;

public record GetWebhookDeliveriesQuery(
    Guid WebhookId,
    WebhookDeliveryStatus? Status = null,
    int PageSize = 50,
    int PageNumber = 1
) : IRequest<Result<PagedResult<TenantWebhookDelivery>>>;

public record GetFailedWebhookDeliveriesQuery(
    Guid TenantId,
    int PageSize = 50,
    int PageNumber = 1
) : IRequest<Result<PagedResult<TenantWebhookDelivery>>>;

public record GetWebhookStatisticsQuery(Guid WebhookId) : IRequest<Result<WebhookStatistics>>;

public record TestTenantWebhookCommand(
    Guid WebhookId,
    object? TestPayload = null
) : IRequest<Result<TenantWebhookDelivery>>;

public record RetryFailedWebhookCommand(
    Guid DeliveryId
) : IRequest<Result<TenantWebhookDelivery>>;

public record EnableTenantWebhookCommand(
    Guid WebhookId
) : IRequest<Result<TenantWebhook>>;

public record DisableTenantWebhookCommand(
    Guid WebhookId
) : IRequest<Result<TenantWebhook>>;

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
