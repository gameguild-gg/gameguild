using GameGuild.Billing.Abstractions;
using GameGuild.Billing.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Billing.Repositories;

/// <summary>
///     Repository implementation for billing webhook events
/// </summary>
public abstract class BillingWebhookRepository(ILogger<BillingWebhookRepository> logger) : IBillingWebhookRepository
{
    // TODO: Inject DbContext when BillingDbContext is created
    // private readonly BillingDbContext _context;

    /// <inheritdoc />
    public async Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting webhook event by ID: {Id}", id);

            // TODO: return await _context.BillingWebhookEvents.FindAsync(new object[] { id }, cancellationToken);
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting webhook event by ID: {Id}", id);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BillingWebhookEvent?> GetByExternalEventIdAsync(string externalEventId, string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting webhook event by external ID: {ExternalEventId} for provider: {Provider}", externalEventId, provider);

            // TODO: return await _context.BillingWebhookEvents
            //     .FirstOrDefaultAsync(e => e.ExternalEventId == externalEventId && e.Provider == provider, cancellationToken);
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting webhook event by external ID: {ExternalEventId}", externalEventId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BillingWebhookEvent>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting webhook events for provider: {Provider}", provider);

            // TODO: return await _context.BillingWebhookEvents
            //     .Where(e => e.Provider == provider)
            //     .OrderByDescending(e => e.CreatedAt)
            //     .ToListAsync(cancellationToken);
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting webhook events for provider: {Provider}", provider);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BillingWebhookEvent>> GetFailedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Getting failed webhook events with max attempts: {MaxAttempts}", maxAttempts);

            // TODO: return await _context.BillingWebhookEvents
            //     .Where(e => e.IsFailed && e.ProcessingAttempts < maxAttempts)
            //     .OrderBy(e => e.CreatedAt)
            //     .ToListAsync(cancellationToken);
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting failed webhook events");

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BillingWebhookEvent> CreateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Creating webhook event: {ExternalEventId} for provider: {Provider}", webhookEvent.ExternalEventId, webhookEvent.Provider);

            // TODO: await _context.BillingWebhookEvents.AddAsync(webhookEvent, cancellationToken);
            // TODO: await _context.SaveChangesAsync(cancellationToken);
            // return webhookEvent;
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating webhook event");

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BillingWebhookEvent> UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Updating webhook event: {Id}", webhookEvent.Id);

            // TODO: _context.BillingWebhookEvents.Update(webhookEvent);
            // TODO: await _context.SaveChangesAsync(cancellationToken);
            // return webhookEvent;
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating webhook event: {Id}", webhookEvent.Id);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Deleting webhook event: {Id}", id);

            // TODO: var webhookEvent = await _context.BillingWebhookEvents.FindAsync(new object[] { id }, cancellationToken);
            // TODO: if (webhookEvent != null)
            // TODO: {
            // TODO:     _context.BillingWebhookEvents.Remove(webhookEvent);
            // TODO:     await _context.SaveChangesAsync(cancellationToken);
            // TODO: }
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting webhook event: {Id}", id);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string externalEventId, string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Checking if webhook event exists: {ExternalEventId} for provider: {Provider}", externalEventId, provider);

            // TODO: return await _context.BillingWebhookEvents
            //     .AnyAsync(e => e.ExternalEventId == externalEventId && e.Provider == provider, cancellationToken);
            throw new NotImplementedException("TODO: Inject DbContext");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if webhook event exists");

            throw;
        }
    }
}
