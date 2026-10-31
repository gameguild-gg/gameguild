namespace GameGuild.Commerce.Billing;

/// <summary>
///     Decorates the billing webhook inbox repository so every webhook event journals its
///     lifecycle into the Compliance.Audit module (issue #408): a <c>WebhookEventReceived</c>
///     entry for every newly accepted delivery and a <c>WebhookEventProcessed</c> /
///     <c>WebhookEventFailed</c> entry for every terminal outcome or failed attempt. The
///     decorator wraps all five provider services and the retry worker uniformly because it
///     sits on the one choke point every inbox row passes through. Journaling is best-effort:
///     the underlying persistence result is always returned unchanged.
/// </summary>
public sealed class AuditingBillingWebhookRepository(
    IBillingWebhookRepository inner,
    IWebhookLifecycleAuditPublisher lifecycleAuditPublisher) : IBillingWebhookRepository
{
    /// <inheritdoc />
    public Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<BillingWebhookEvent?> GetByExternalEventIdAsync(string externalEventId, string provider, CancellationToken cancellationToken = default) =>
        inner.GetByExternalEventIdAsync(externalEventId, provider, cancellationToken);

    /// <inheritdoc />
    public Task<BillingWebhookEvent?> GetByProviderScopeAsync(
        string provider,
        string providerEnvironment,
        string providerAccountId,
        string webhookEndpointId,
        string externalEventId,
        CancellationToken cancellationToken = default) =>
        inner.GetByProviderScopeAsync(provider, providerEnvironment, providerAccountId, webhookEndpointId, externalEventId, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<BillingWebhookEvent>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default) =>
        inner.GetByProviderAsync(provider, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<BillingWebhookEvent>> GetFailedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default) =>
        inner.GetFailedEventsAsync(maxAttempts, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<BillingWebhookEvent>> GetRetryCandidatesAsync(int maxAttempts, int take, CancellationToken cancellationToken = default) =>
        inner.GetRetryCandidatesAsync(maxAttempts, take, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<BillingWebhookEvent>> GetExhaustedEventsAsync(int maxAttempts, int take, CancellationToken cancellationToken = default) =>
        inner.GetExhaustedEventsAsync(maxAttempts, take, cancellationToken);

    /// <inheritdoc />
    public Task<(IReadOnlyList<BillingWebhookEvent> Items, int TotalCount)> SearchEventsAsync(
        BillingWebhookEventSearchCriteria criteria,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        inner.SearchEventsAsync(criteria, skip, take, cancellationToken);

    /// <inheritdoc />
    public async Task<BillingWebhookEvent> CreateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var persisted = await inner.CreateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

        // Only a genuinely NEW row journals a Received entry: the repository returns a
        // different instance when an idempotency winner already existed (duplicate or
        // concurrent delivery), and those re-acknowledgements are not new events.
        if (ReferenceEquals(persisted, webhookEvent))
        {
            await lifecycleAuditPublisher
                .PublishAsync(WebhookLifecycleOutcome.Received, persisted, cancellationToken)
                .ConfigureAwait(false);
        }

        return persisted;
    }

    /// <inheritdoc />
    public Task<bool> TryClaimProcessingAsync(BillingWebhookEvent webhookEvent, DateTime staleBefore, CancellationToken cancellationToken = default) =>
        inner.TryClaimProcessingAsync(webhookEvent, staleBefore, cancellationToken);

    /// <inheritdoc />
    public async Task<BillingWebhookEvent> UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var persisted = await inner.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

        // Terminal outcomes and failed attempts journal; intermediate saves (claim
        // bookkeeping, signature-verified stamping with no state change) stay quiet so each
        // event yields exactly one Received → Processed trail plus one Failed per attempt.
        if (persisted.IsProcessed)
        {
            await lifecycleAuditPublisher
                .PublishAsync(WebhookLifecycleOutcome.Processed, persisted, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (persisted.IsFailed)
        {
            await lifecycleAuditPublisher
                .PublishAsync(WebhookLifecycleOutcome.Failed, persisted, cancellationToken)
                .ConfigureAwait(false);
        }

        return persisted;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string externalEventId, string provider, CancellationToken cancellationToken = default) =>
        inner.ExistsAsync(externalEventId, provider, cancellationToken);
}
