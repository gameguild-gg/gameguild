namespace GameGuild.Commerce.Billing;

/// <summary>
///     Repository for managing billing webhook events
/// </summary>
public interface IBillingWebhookRepository
{
    /// <summary>
    ///     Get a webhook event by ID
    /// </summary>
    Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get a webhook event by external event ID
    /// </summary>
    Task<BillingWebhookEvent?> GetByExternalEventIdAsync(string externalEventId, string provider, CancellationToken cancellationToken = default);

    /// <summary>Get an event by its provider-scoped idempotency identity.</summary>
    Task<BillingWebhookEvent?> GetByProviderScopeAsync(
        string provider,
        string providerEnvironment,
        string providerAccountId,
        string webhookEndpointId,
        string externalEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all webhook events for a provider
    /// </summary>
    Task<IEnumerable<BillingWebhookEvent>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get failed webhook events that need retry
    /// </summary>
    Task<IEnumerable<BillingWebhookEvent>> GetFailedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new webhook event
    /// </summary>
    Task<BillingWebhookEvent> CreateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Atomically claims an accepted webhook for processing.
    /// </summary>
    Task<bool> TryClaimProcessingAsync(
        BillingWebhookEvent webhookEvent,
        DateTime staleBefore,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing webhook event
    /// </summary>
    Task<BillingWebhookEvent> UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a webhook event
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a webhook event with the given external ID already exists
    /// </summary>
    Task<bool> ExistsAsync(string externalEventId, string provider, CancellationToken cancellationToken = default);
}
