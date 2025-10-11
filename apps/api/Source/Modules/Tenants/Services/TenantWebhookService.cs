using System.Text;
using System.Text.Json;
using GameGuild.Modules.Tenants;
using GameGuild.Tenants.Repositories;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tenants.Services;

/// <summary>
/// Service implementation for managing tenant lifecycle webhooks
/// </summary>
public class TenantWebhookService : ITenantWebhookService {
    private readonly ITenantWebhookRepository _webhookRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantWebhookService> _logger;

    public TenantWebhookService(
        ITenantWebhookRepository webhookRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantWebhookService> logger) {
        _webhookRepository = webhookRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TenantWebhook> RegisterWebhookAsync(
        Guid tenantId,
        string url,
        TenantWebhookEventType eventType,
        string? secret = null,
        int retryCount = 3,
        int timeoutSeconds = 30,
        string? headers = null,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Registering webhook for tenant {TenantId}, event type {EventType}", tenantId, eventType);

        var webhook = new TenantWebhook(url, tenantId, eventType, secret, retryCount, timeoutSeconds, headers);
        var created = await _webhookRepository.CreateAsync(webhook, cancellationToken);

        _logger.LogInformation("Webhook {WebhookId} registered successfully", created.Id);
        return created;
    }

    public async Task<TenantWebhook> UpdateWebhookAsync(
        Guid webhookId,
        string? url = null,
        string? secret = null,
        int? retryCount = null,
        int? timeoutSeconds = null,
        string? headers = null,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Updating webhook {WebhookId}", webhookId);

        var webhook = await _webhookRepository.GetByIdAsync(webhookId, cancellationToken)
            ?? throw new InvalidOperationException($"Webhook {webhookId} not found");

        if (url != null) webhook.UpdateUrl(url);
        if (secret != null) webhook.UpdateSecret(secret);
        if (retryCount.HasValue) webhook.UpdateRetryCount(retryCount.Value);
        if (timeoutSeconds.HasValue) webhook.UpdateTimeoutSeconds(timeoutSeconds.Value);
        if (headers != null) webhook.UpdateHeaders(headers);

        var updated = await _webhookRepository.UpdateAsync(webhook, cancellationToken);

        _logger.LogInformation("Webhook {WebhookId} updated successfully", webhookId);
        return updated;
    }

    public async Task<bool> DeleteWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default) {
        _logger.LogInformation("Deleting webhook {WebhookId}", webhookId);

        var result = await _webhookRepository.DeleteAsync(webhookId, cancellationToken);

        if (!result) {
            _logger.LogWarning("Webhook {WebhookId} not found", webhookId);
            return false;
        }

        _logger.LogInformation("Webhook {WebhookId} deleted successfully", webhookId);
        return true;
    }

    public async Task<TenantWebhook> ActivateWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default) {
        _logger.LogInformation("Activating webhook {WebhookId}", webhookId);

        var webhook = await _webhookRepository.GetByIdAsync(webhookId, cancellationToken)
            ?? throw new InvalidOperationException($"Webhook {webhookId} not found");

        webhook.Activate();
        var updated = await _webhookRepository.UpdateAsync(webhook, cancellationToken);

        _logger.LogInformation("Webhook {WebhookId} activated successfully", webhookId);
        return updated;
    }

    public async Task<TenantWebhook> DeactivateWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default) {
        _logger.LogInformation("Deactivating webhook {WebhookId}", webhookId);

        var webhook = await _webhookRepository.GetByIdAsync(webhookId, cancellationToken)
            ?? throw new InvalidOperationException($"Webhook {webhookId} not found");

        webhook.Deactivate();
        var updated = await _webhookRepository.UpdateAsync(webhook, cancellationToken);

        _logger.LogInformation("Webhook {WebhookId} deactivated successfully", webhookId);
        return updated;
    }

    public async Task<int> TriggerWebhooksAsync(
        Guid tenantId,
        TenantWebhookEventType eventType,
        object payload,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Triggering webhooks for tenant {TenantId}, event type {EventType}", tenantId, eventType);

        var webhooks = await _webhookRepository.GetActiveForEventAsync(tenantId, eventType, cancellationToken);
        var triggeredCount = 0;

        foreach (var webhook in webhooks) {
            try {
                await DeliverWebhookAsync(webhook, eventType, payload, cancellationToken);
                triggeredCount++;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to trigger webhook {WebhookId}", webhook.Id);
            }
        }

        _logger.LogInformation("Triggered {Count} webhooks for tenant {TenantId}", triggeredCount, tenantId);
        return triggeredCount;
    }

    public async Task<IEnumerable<TenantWebhook>> GetTenantWebhooksAsync(
        Guid tenantId,
        TenantWebhookEventType? eventType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default) {
        if (eventType.HasValue) {
            return await _webhookRepository.GetActiveForEventAsync(tenantId, eventType.Value, cancellationToken);
        }
        return await _webhookRepository.GetByTenantIdAsync(tenantId, isActive, cancellationToken);
    }

    public async Task<IEnumerable<TenantWebhookDelivery>> GetWebhookDeliveriesAsync(
        Guid webhookId,
        WebhookDeliveryStatus? status = null,
        int pageSize = 50,
        int pageNumber = 1,
        CancellationToken cancellationToken = default) {
        bool? success = status.HasValue ? status.Value == WebhookDeliveryStatus.Delivered : null;
        var (deliveries, _) = await _webhookRepository.GetDeliveriesAsync(webhookId, success, null, null, pageNumber, pageSize, cancellationToken);
        return deliveries;
    }

    public async Task<WebhookStatistics> GetWebhookStatisticsAsync(Guid webhookId, CancellationToken cancellationToken = default) {
        var (deliveries, _) = await _webhookRepository.GetDeliveriesAsync(webhookId, null, null, null, 1, 1000, cancellationToken);
        var deliveryList = deliveries.ToList();

        var totalDeliveries = deliveryList.Count;
        var successfulDeliveries = deliveryList.Count(d => d.Status == WebhookDeliveryStatus.Delivered);
        var failedDeliveries = deliveryList.Count(d => d.Status == WebhookDeliveryStatus.Failed || d.Status == WebhookDeliveryStatus.Expired);
        var pendingDeliveries = deliveryList.Count(d => d.Status == WebhookDeliveryStatus.Pending || d.Status == WebhookDeliveryStatus.Retrying);
        var successRate = totalDeliveries > 0 ? (double)successfulDeliveries / totalDeliveries * 100 : 0;

        var lastTriggeredAt = deliveryList.OrderByDescending(d => d.CreatedAt).FirstOrDefault()?.CreatedAt;
        var lastSuccessAt = deliveryList.Where(d => d.Status == WebhookDeliveryStatus.Delivered).OrderByDescending(d => d.DeliveredAt).FirstOrDefault()?.DeliveredAt;
        var lastFailureAt = deliveryList.Where(d => d.Status == WebhookDeliveryStatus.Failed).OrderByDescending(d => d.UpdatedAt).FirstOrDefault()?.UpdatedAt;

        return new WebhookStatistics(
            webhookId,
            totalDeliveries,
            successfulDeliveries,
            failedDeliveries,
            pendingDeliveries,
            successRate,
            lastTriggeredAt,
            lastSuccessAt,
            lastFailureAt
        );
    }

    private async Task DeliverWebhookAsync(
        TenantWebhook webhook,
        TenantWebhookEventType eventType,
        object payload,
        CancellationToken cancellationToken) {
        var payloadJson = JsonSerializer.Serialize(payload);
        var delivery = new TenantWebhookDelivery(webhook.Id, eventType, payloadJson);

        // Record the delivery attempt
        delivery = await _webhookRepository.RecordDeliveryAsync(delivery, cancellationToken);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(webhook.TimeoutSeconds);

        for (var attempt = 0; attempt < webhook.RetryCount; attempt++) {
            try {
                delivery.RecordAttempt();
                await _webhookRepository.RecordDeliveryAsync(delivery, cancellationToken);

                var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url) {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };

                // Add custom headers
                if (!string.IsNullOrEmpty(webhook.Headers)) {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(webhook.Headers);
                    if (headers != null) {
                        foreach (var header in headers) {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                // Add signature if secret is provided
                if (!string.IsNullOrEmpty(webhook.Secret)) {
                    var signature = GenerateSignature(payloadJson, webhook.Secret);
                    request.Headers.Add("X-Webhook-Signature", signature);
                }

                var response = await httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode) {
                    delivery.MarkAsDelivered((int)response.StatusCode, responseBody);
                    webhook.RecordSuccess();
                    await _webhookRepository.RecordDeliveryAsync(delivery, cancellationToken);
                    await _webhookRepository.UpdateAsync(webhook, cancellationToken);

                    _logger.LogInformation("Webhook {WebhookId} delivered successfully", webhook.Id);
                    return;
                }

                var errorMessage = $"HTTP {response.StatusCode}: {responseBody}";
                var nextRetryAt = attempt < webhook.RetryCount - 1 ? DateTime.UtcNow.AddMinutes(Math.Pow(2, attempt)) : (DateTime?)null;
                delivery.MarkAsFailed(errorMessage, nextRetryAt);
                webhook.RecordFailure();

                await _webhookRepository.RecordDeliveryAsync(delivery, cancellationToken);
                await _webhookRepository.UpdateAsync(webhook, cancellationToken);

                if (nextRetryAt.HasValue) {
                    _logger.LogWarning("Webhook {WebhookId} delivery failed, retrying at {NextRetryAt}", webhook.Id, nextRetryAt);
                    await Task.Delay(TimeSpan.FromMinutes(Math.Pow(2, attempt)), cancellationToken);
                }
            }
            catch (Exception ex) {
                var nextRetryAt = attempt < webhook.RetryCount - 1 ? DateTime.UtcNow.AddMinutes(Math.Pow(2, attempt)) : (DateTime?)null;
                delivery.MarkAsFailed(ex.Message, nextRetryAt);
                webhook.RecordFailure();

                await _webhookRepository.RecordDeliveryAsync(delivery, cancellationToken);
                await _webhookRepository.UpdateAsync(webhook, cancellationToken);

                _logger.LogError(ex, "Webhook {WebhookId} delivery attempt {Attempt} failed", webhook.Id, attempt + 1);

                if (nextRetryAt.HasValue) {
                    await Task.Delay(TimeSpan.FromMinutes(Math.Pow(2, attempt)), cancellationToken);
                }
            }
        }

        // Mark as expired after all retries exhausted
        delivery.MarkAsExpired();
        await _webhookRepository.RecordDeliveryAsync(delivery, cancellationToken);

        _logger.LogError("Webhook {WebhookId} delivery failed after {RetryCount} attempts", webhook.Id, webhook.RetryCount);
    }

    private static string GenerateSignature(string payload, string secret) {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
