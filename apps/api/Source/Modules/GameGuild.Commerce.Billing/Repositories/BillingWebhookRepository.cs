using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Repository implementation for billing webhook events
/// </summary>
public abstract class BillingWebhookRepository(ILogger<BillingWebhookRepository> logger) : IBillingWebhookRepository
{
    // TODO: Inject DbContext when BillingDbContext is created
    // private readonly BillingDbContext _context;

    /// <inheritdoc />
    public Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting webhook event by ID: {Id}", id);

        // TODO: return await _context.BillingWebhookEvents.FindAsync(new object[] { id }, cancellationToken);
        return Task.FromException<BillingWebhookEvent?>(new NotImplementedException("TODO: Inject DbContext"));
    }

    /// <inheritdoc />
    public Task<BillingWebhookEvent?> GetByExternalEventIdAsync(string externalEventId, string provider, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting webhook event by external ID: {ExternalEventId} for provider: {Provider}", externalEventId, provider);

        // TODO: return await _context.BillingWebhookEvents
        //     .FirstOrDefaultAsync(e => e.ExternalEventId == externalEventId && e.Provider == provider, cancellationToken);
        return Task.FromException<BillingWebhookEvent?>(new NotImplementedException("TODO: Inject DbContext"));
    }

    /// <inheritdoc />
    public Task<IEnumerable<BillingWebhookEvent>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting webhook events for provider: {Provider}", provider);

        // TODO: return await _context.BillingWebhookEvents
        //     .Where(e => e.Provider == provider)
        //     .OrderByDescending(e => e.CreatedAt)
        //     .ToListAsync(cancellationToken);
        return Task.FromException<IEnumerable<BillingWebhookEvent>>(new NotImplementedException("TODO: Inject DbContext"));
    }

    /// <inheritdoc />
    public Task<IEnumerable<BillingWebhookEvent>> GetFailedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting failed webhook events with max attempts: {MaxAttempts}", maxAttempts);

        // TODO: return await _context.BillingWebhookEvents
        //     .Where(e => e.IsFailed && e.ProcessingAttempts < maxAttempts)
        //     .OrderBy(e => e.CreatedAt)
        //     .ToListAsync(cancellationToken);
        return Task.FromException<IEnumerable<BillingWebhookEvent>>(new NotImplementedException("TODO: Inject DbContext"));
    }

    /// <inheritdoc />
    public Task<BillingWebhookEvent> CreateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating webhook event: {ExternalEventId} for provider: {Provider}", webhookEvent.ExternalEventId, webhookEvent.Provider);

        // TODO: await _context.BillingWebhookEvents.AddAsync(webhookEvent, cancellationToken);
        // TODO: await _context.SaveChangesAsync(cancellationToken);
        // return webhookEvent;
        return Task.FromException<BillingWebhookEvent>(new NotImplementedException("TODO: Inject DbContext"));
    }

    /// <inheritdoc />
    public Task<BillingWebhookEvent> UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating webhook event: {Id}", webhookEvent.Id);

        // TODO: _context.BillingWebhookEvents.Update(webhookEvent);
        // TODO: await _context.SaveChangesAsync(cancellationToken);
        // return webhookEvent;
        return Task.FromException<BillingWebhookEvent>(new NotImplementedException("TODO: Inject DbContext"));
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting webhook event: {Id}", id);

        // TODO: var webhookEvent = await _context.BillingWebhookEvents.FindAsync(new object[] { id }, cancellationToken);
        // TODO: if (webhookEvent != null)
        // TODO: {
        // TODO:     _context.BillingWebhookEvents.Remove(webhookEvent);
        // TODO:     await _context.SaveChangesAsync(cancellationToken);
        // TODO: }
        return Task.FromException(new NotImplementedException("TODO: Inject DbContext"));
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string externalEventId, string provider, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking if webhook event exists: {ExternalEventId} for provider: {Provider}", externalEventId, provider);

        // TODO: return await _context.BillingWebhookEvents
        //     .AnyAsync(e => e.ExternalEventId == externalEventId && e.Provider == provider, cancellationToken);
        return Task.FromException<bool>(new NotImplementedException("TODO: Inject DbContext"));
    }
}
