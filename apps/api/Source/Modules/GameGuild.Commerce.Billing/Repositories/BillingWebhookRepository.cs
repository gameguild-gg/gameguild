using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Repository implementation for billing webhook events.
///     Uses ExternalEventId as the idempotency key to prevent duplicate processing.
/// </summary>
public class BillingWebhookRepository(IApplicationDbContext context, ILogger<BillingWebhookRepository> logger) 
    : CommerceRepositoryBase<BillingWebhookEvent>(context), IBillingWebhookRepository
{
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
            .Where(e => e.IsFailed && e.ProcessingAttempts < maxAttempts)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
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
        await Entities.AddAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return webhookEvent;
    }

    /// <inheritdoc />
    public new async Task<BillingWebhookEvent> UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);
        logger.LogInformation("Updating webhook event: {Id}", webhookEvent.Id);
        
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
}
