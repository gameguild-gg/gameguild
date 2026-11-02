using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Repository implementation for billing webhook events.
///     Uses ExternalEventId as the idempotency key to prevent duplicate processing.
///     When a new inbox row is persisted inside a provider callback request, the delivery
///     source captured by <see cref="WebhookSourceSecurityFilter"/> (remote IP + user agent)
///     is stamped onto the row (issue #408: IP address and user agent logging).
///     When the opt-in raw-delivery retention policy
///     (<see cref="WebhookSettings.Retention"/>) is enabled, the row's redacted raw payload
///     and full header set are persisted alongside the minimized record — but only once the
///     delivery's signature verification has succeeded (issue #408: capture raw payload and
///     all headers; verify FIRST, then store the redacted form).
/// </summary>
public class BillingWebhookRepository(
    IApplicationDbContext context,
    ILogger<BillingWebhookRepository> logger,
    IWebhookDeliveryContext? deliveryContext = null,
    IOptions<BillingConfiguration>? billingConfiguration = null)
    : CommerceRepositoryBase<BillingWebhookEvent>(context), IBillingWebhookRepository
{
    private static readonly HashSet<string> WebhookIdempotencyIndexes = new(StringComparer.Ordinal)
    {
        "ix_billing_webhook_events_external_id_provider",
        "ix_billing_webhook_events_provider_scope_event"
    };

    private readonly WebhookRetentionPolicy _retention = billingConfiguration?.Value.Webhook.Retention ?? new WebhookRetentionPolicy();
    /// <inheritdoc />
    public new async Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting webhook event by ID: {Id}", id);
        return await Entities.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BillingWebhookEvent?> GetByExternalEventIdAsync(string externalEventId, string provider, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting webhook event by external ID: {ExternalEventId} for provider: {Provider}", externalEventId, provider);
        return await Entities
            .FirstOrDefaultAsync(e => e.ExternalEventId == externalEventId && e.Provider == provider, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BillingWebhookEvent?> GetByProviderScopeAsync(
        string provider,
        string providerEnvironment,
        string providerAccountId,
        string webhookEndpointId,
        string externalEventId,
        CancellationToken cancellationToken = default)
    {
        return await Entities.FirstOrDefaultAsync(
                webhookEvent => webhookEvent.Provider == provider &&
                                webhookEvent.ProviderEnvironment == providerEnvironment &&
                                webhookEvent.ProviderAccountId == providerAccountId &&
                                webhookEvent.WebhookEndpointId == webhookEndpointId &&
                                webhookEvent.ExternalEventId == externalEventId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BillingWebhookEvent>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting webhook events for provider: {Provider}", provider);
        return await Entities
            .Where(e => e.Provider == provider)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BillingWebhookEvent>> GetFailedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting failed webhook events with max attempts: {MaxAttempts}", maxAttempts);
        return await Entities
            .Where(e => e.IsFailed && e.DeadLetteredAt == null && e.ProcessingAttempts < maxAttempts)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BillingWebhookEvent>> GetRetryCandidatesAsync(
        int maxAttempts,
        int take,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Getting retry candidates with max attempts {MaxAttempts}, take {Take}",
            maxAttempts,
            take);
        return await Entities
            .Where(e => e.IsFailed && !e.IsProcessed && e.DeadLetteredAt == null && e.ProcessingAttempts < maxAttempts)
            .OrderBy(e => e.UpdatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BillingWebhookEvent>> GetExhaustedEventsAsync(
        int maxAttempts,
        int take,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Getting exhausted webhook events with max attempts {MaxAttempts}, take {Take}",
            maxAttempts,
            take);
        return await Entities
            .Where(e => e.IsFailed && !e.IsProcessed && e.DeadLetteredAt == null && e.ProcessingAttempts >= maxAttempts)
            .OrderBy(e => e.UpdatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<BillingWebhookEvent> Items, int TotalCount)> SearchEventsAsync(
        BillingWebhookEventSearchCriteria criteria,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = Entities.AsQueryable();

        query = criteria.Status switch
        {
            BillingEventStatusFilter.Processed => query.Where(e => e.IsProcessed && !e.IsFailed),
            BillingEventStatusFilter.Failed => query.Where(e => e.IsFailed && e.DeadLetteredAt == null),
            BillingEventStatusFilter.DeadLettered => query.Where(e => e.DeadLetteredAt != null),
            BillingEventStatusFilter.Pending => query.Where(e => !e.IsProcessed && !e.IsFailed),
            _ => query
        };

        if (criteria.Provider is not null)
        {
            query = query.Where(e => e.Provider == criteria.Provider);
        }

        if (criteria.EventType is not null)
        {
            query = query.Where(e => e.EventType == criteria.EventType);
        }

        if (criteria.FromUtc.HasValue)
        {
            var fromUtc = criteria.FromUtc.Value;
            query = query.Where(e => e.CreatedAt >= fromUtc);
        }

        if (criteria.ToUtc.HasValue)
        {
            var toUtc = criteria.ToUtc.Value;
            query = query.Where(e => e.CreatedAt <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public new async Task<BillingWebhookEvent> CreateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);
        
        var existingEvent = webhookEvent.ProviderEnvironment is not null &&
                            webhookEvent.ProviderAccountId is not null &&
                            webhookEvent.WebhookEndpointId is not null
            ? await GetByProviderScopeAsync(
                    webhookEvent.Provider,
                    webhookEvent.ProviderEnvironment,
                    webhookEvent.ProviderAccountId,
                    webhookEvent.WebhookEndpointId,
                    webhookEvent.ExternalEventId,
                    cancellationToken)
                .ConfigureAwait(false)
            : await GetByExternalEventIdAsync(webhookEvent.ExternalEventId, webhookEvent.Provider, cancellationToken)
                .ConfigureAwait(false);
        if (existingEvent is not null)
        {
            logger.LogWarning("Duplicate webhook event detected: {ExternalEventId} for provider: {Provider}. Returning existing event.", 
                webhookEvent.ExternalEventId, webhookEvent.Provider);
            return existingEvent;
        }
        
        logger.LogInformation("Creating webhook event: {ExternalEventId} for provider: {Provider}", webhookEvent.ExternalEventId, webhookEvent.Provider);

        // Stamp the delivery source captured by the endpoint security filter (issue #408):
        // remote IP + user agent of the callback request. Outside a callback request (retry
        // worker, replays) the context is empty and the row keeps its original source.
        if (deliveryContext is not null
            && (string.IsNullOrWhiteSpace(webhookEvent.SourceIpAddress) || string.IsNullOrWhiteSpace(webhookEvent.UserAgent)))
        {
            webhookEvent.RecordDeliverySource(deliveryContext.SourceIpAddress, deliveryContext.UserAgent);
        }

        // Opt-in raw-delivery retention (issue #408: capture raw payload and all headers):
        // queues the redacted raw delivery in the same unit of work as the minimized inbox
        // row. Guards inside: retention enabled, delivery verified, raw capture present and
        // not already retained.
        await TryPersistRawDeliveryAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

        await Entities.AddAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
        try
        {
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return webhookEvent;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            Entities.Remove(webhookEvent);

            // Retention bookkeeping (issue #408): the loser row never reached the database,
            // so its queued raw delivery must leave the change tracker too — otherwise a
            // later SaveChanges in this scope would insert a raw row with a dangling
            // reference to an inbox row that does not exist.
            if (Context is DbContext dbContext)
            {
                var queuedRawDelivery = dbContext.ChangeTracker.Entries<BillingWebhookRawDelivery>()
                    .FirstOrDefault(entry => entry.State == EntityState.Added &&
                                             entry.Entity.BillingWebhookEventId == webhookEvent.Id);
                if (queuedRawDelivery is not null)
                {
                    Context.Set<BillingWebhookRawDelivery>().Remove(queuedRawDelivery.Entity);
                }
            }

            var concurrentWinner = webhookEvent.ProviderEnvironment is not null &&
                                   webhookEvent.ProviderAccountId is not null &&
                                   webhookEvent.WebhookEndpointId is not null
                ? await GetByProviderScopeAsync(
                        webhookEvent.Provider,
                        webhookEvent.ProviderEnvironment,
                        webhookEvent.ProviderAccountId,
                        webhookEvent.WebhookEndpointId,
                        webhookEvent.ExternalEventId,
                        cancellationToken)
                    .ConfigureAwait(false)
                : await GetByExternalEventIdAsync(
                        webhookEvent.ExternalEventId,
                        webhookEvent.Provider,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (concurrentWinner is not null)
            {
                logger.LogWarning(
                    "Concurrent duplicate webhook event detected: {ExternalEventId} for provider: {Provider}. Returning winner.",
                    webhookEvent.ExternalEventId,
                    webhookEvent.Provider);
                return concurrentWinner;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryClaimProcessingAsync(
        BillingWebhookEvent webhookEvent,
        DateTime staleBefore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        if (!webhookEvent.TryBeginProcessing(staleBefore))
        {
            return false;
        }

        Entities.Update(webhookEvent);
        try
        {
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                "Webhook processing claim lost for {ExternalEventId} from {Provider}.",
                webhookEvent.ExternalEventId,
                webhookEvent.Provider);
            return false;
        }
    }

    /// <inheritdoc />
    public new async Task<BillingWebhookEvent> UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);
        logger.LogInformation("Updating webhook event: {Id}", webhookEvent.Id);

        // Raw-delivery retention (issue #408): providers that persist the inbox row before
        // verifying (PayPal) stamp IsSignatureVerified on this update path, so the redacted
        // raw delivery is persisted here — after verification — on the first update that
        // carries the proof. Providers that verify first already retained it at creation.
        await TryPersistRawDeliveryAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

        Entities.Update(webhookEvent);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return webhookEvent;
    }

    /// <inheritdoc />
    public new async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting webhook event: {Id}", id);
        
        var webhookEvent = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (webhookEvent is not null)
        {
            Entities.Remove(webhookEvent);
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string externalEventId, string provider, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking if webhook event exists: {ExternalEventId} for provider: {Provider}", externalEventId, provider);
        return await Entities
            .AnyAsync(e => e.ExternalEventId == externalEventId && e.Provider == provider, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> PurgeExpiredRawDeliveriesAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (!_retention.Enabled)
        {
            return 0;
        }

        var rawDeliveries = Context.Set<BillingWebhookRawDelivery>();
        var expired = await rawDeliveries
            .Where(delivery => delivery.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expired.Count == 0)
        {
            return 0;
        }

        rawDeliveries.RemoveRange(expired);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Purged {Count} expired raw webhook delivery record(s) past their retention window.",
            expired.Count);
        return expired.Count;
    }

    /// <summary>
    ///     Queues the redacted raw delivery for the given inbox row, subject to every
    ///     retention guard (issue #408: capture raw payload and all headers):
    ///     <list type="number">
    ///         <item>the opt-in policy must be enabled (default OFF — no-op in production);</item>
    ///         <item>the delivery must be signature-VERIFIED first — the redacted form is only ever stored after verification succeeded;</item>
    ///         <item>the request must carry a raw capture (a provider callback, not the retry worker);</item>
    ///         <item>at most one raw delivery per inbox row.</item>
    ///     </list>
    ///     The stored form is produced by <see cref="WebhookRawDeliveryRedactor"/>: secrets
    ///     (tokens, authorization and signature headers) become SHA-256 hashes, emails
    ///     become deterministic tokens when PII redaction is on — nothing is stored literally.
    /// </summary>
    private async Task TryPersistRawDeliveryAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (!_retention.Enabled)
        {
            return;
        }

        // Verify FIRST, then store: rows without the verification proof (PayPal's inbox row
        // is persisted before its verification call completes) never retain raw bytes.
        if (webhookEvent.IsSignatureVerified != true)
        {
            return;
        }

        if (deliveryContext?.RawPayload is not { Length: > 0 } rawPayload)
        {
            return;
        }

        var alreadyRetained = await Context.Set<BillingWebhookRawDelivery>()
            .AnyAsync(delivery => delivery.BillingWebhookEventId == webhookEvent.Id, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyRetained)
        {
            return;
        }

        var receivedAt = SystemClock.UtcNow;
        var redactedPayload = WebhookRawDeliveryRedactor.RedactPayload(rawPayload, _retention.RedactPii);
        var redactedHeaders = WebhookRawDeliveryRedactor.RedactHeaders(
            deliveryContext.RawHeaders ?? [],
            _retention.RedactPii);

        var boundedRetentionDays = Math.Clamp(
            _retention.RetentionDays,
            WebhookRetentionPolicy.MinRetentionDays,
            WebhookRetentionPolicy.MaxRetentionDays);

        var rawDelivery = new BillingWebhookRawDelivery
        {
            BillingWebhookEventId = webhookEvent.Id,
            Provider = webhookEvent.Provider,
            ExternalEventId = webhookEvent.ExternalEventId,
            RawPayload = redactedPayload,
            RawHeaders = redactedHeaders,
            // Hash of the ORIGINAL payload bytes — intake evidence computed before
            // redaction so the retained row can be matched against the provider's
            // delivery material by anyone holding the raw value.
            PayloadSha256 = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(rawPayload))).ToLowerInvariant(),
            PiiRedacted = _retention.RedactPii,
            ReceivedAt = receivedAt,
            ExpiresAt = receivedAt.AddDays(boundedRetentionDays)
        };

        Context.Set<BillingWebhookRawDelivery>().Add(rawDelivery);

        logger.LogInformation(
            "Retained redacted raw delivery for webhook event {EventId} (provider {Provider}) with a {Days}-day retention window.",
            webhookEvent.ExternalEventId,
            webhookEvent.Provider,
            boundedRetentionDays);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is not DbException { SqlState: "23505" } databaseException)
            {
                continue;
            }

            var constraintName = databaseException.GetType()
                .GetProperty("ConstraintName")
                ?.GetValue(databaseException) as string;
            return constraintName is not null &&
                   WebhookIdempotencyIndexes.Contains(constraintName);
        }

        return false;
    }
}
