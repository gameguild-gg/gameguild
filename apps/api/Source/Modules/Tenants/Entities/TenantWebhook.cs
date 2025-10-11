using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents a webhook subscription for tenant lifecycle events
/// </summary>
[Table("TenantWebhooks")]
[Index(nameof(TenantId), nameof(IsActive))]
[Index(nameof(EventType), nameof(IsActive))]
public class TenantWebhook : EntityBase
{
    [Required]
    [MaxLength(200)]
    public string Url { get; private set; } = string.Empty;

    [Required]
    public Guid TenantId { get; private set; }

    [Required]
    [MaxLength(50)]
    public TenantWebhookEventType EventType { get; private set; }

    [Required]
    [MaxLength(500)]
    public string? Secret { get; private set; }

    [Required]
    public bool IsActive { get; private set; } = true;

    [Required]
    public int RetryCount { get; private set; } = 3;

    [Required]
    public int TimeoutSeconds { get; private set; } = 30;

    public DateTime? LastTriggeredAt { get; private set; }

    public int SuccessCount { get; private set; }

    public int FailureCount { get; private set; }

    [MaxLength(2000)]
    public string? Headers { get; private set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; private set; }

    // Constructors
    private TenantWebhook() { }

    public TenantWebhook(
        string url,
        Guid tenantId,
        TenantWebhookEventType eventType,
        string? secret = null,
        int retryCount = 3,
        int timeoutSeconds = 30,
        string? headers = null)
    {
        Url = url;
        TenantId = tenantId;
        EventType = eventType;
        Secret = secret;
        RetryCount = retryCount;
        TimeoutSeconds = timeoutSeconds;
        Headers = headers;
        IsActive = true;
    }

    // Domain methods
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void RecordSuccess()
    {
        SuccessCount++;
        LastTriggeredAt = DateTime.UtcNow;
    }

    public void RecordFailure()
    {
        FailureCount++;
        LastTriggeredAt = DateTime.UtcNow;
    }

    public void UpdateUrl(string url) => Url = url;
    public void UpdateSecret(string? secret) => Secret = secret;
    public void UpdateRetryCount(int retryCount) => RetryCount = retryCount;
    public void UpdateTimeoutSeconds(int timeoutSeconds) => TimeoutSeconds = timeoutSeconds;
    public void UpdateHeaders(string? headers) => Headers = headers;
}

/// <summary>
/// Represents the type of tenant lifecycle event
/// </summary>
public enum TenantWebhookEventType
{
    Created = 1,
    Updated = 2,
    Activated = 3,
    Deactivated = 4,
    Suspended = 5,
    Upgraded = 6,
    Downgraded = 7,
    Deleted = 8,
    Restored = 9,
    Archived = 10
}

/// <summary>
/// Represents a tenant webhook delivery log entry
/// </summary>
[Table("TenantWebhookDeliveries")]
[Index(nameof(WebhookId), nameof(CreatedAt))]
[Index(nameof(Status), nameof(CreatedAt))]
public class TenantWebhookDelivery : EntityBase
{
    [Required]
    public Guid WebhookId { get; private set; }

    [Required]
    [MaxLength(50)]
    public TenantWebhookEventType EventType { get; private set; }

    [Required]
    [MaxLength(4000)]
    public string Payload { get; private set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public WebhookDeliveryStatus Status { get; private set; } = WebhookDeliveryStatus.Pending;

    public int AttemptCount { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public int? ResponseStatusCode { get; private set; }

    [MaxLength(4000)]
    public string? ResponseBody { get; private set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    // Navigation properties
    public virtual TenantWebhook? Webhook { get; private set; }

    // Constructors
    private TenantWebhookDelivery() { }

    public TenantWebhookDelivery(
        Guid webhookId,
        TenantWebhookEventType eventType,
        string payload)
    {
        WebhookId = webhookId;
        EventType = eventType;
        Payload = payload;
        Status = WebhookDeliveryStatus.Pending;
        AttemptCount = 0;
    }

    // Domain methods
    public void MarkAsDelivered(int statusCode, string? responseBody)
    {
        Status = WebhookDeliveryStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public void MarkAsFailed(string errorMessage, DateTime? nextRetryAt = null)
    {
        AttemptCount++;
        Status = WebhookDeliveryStatus.Failed;
        ErrorMessage = errorMessage;
        NextRetryAt = nextRetryAt;
    }

    public void MarkAsRetrying()
    {
        AttemptCount++;
        Status = WebhookDeliveryStatus.Retrying;
    }

    public void MarkAsExpired()
    {
        Status = WebhookDeliveryStatus.Expired;
    }
}

public enum WebhookDeliveryStatus
{
    Pending = 1,
    Delivered = 2,
    Failed = 3,
    Retrying = 4,
    Expired = 5
}
