using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tenants.Repositories;

/// <summary>
/// Repository implementation for tenant webhook operations.
/// </summary>
public class TenantWebhookRepository : ITenantWebhookRepository
{
    private readonly DbContext _context;

    public TenantWebhookRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<TenantWebhook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantWebhook>()
            .Include(w => w.Deliveries)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TenantWebhook>> GetByTenantIdAsync(Guid tenantId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TenantWebhook>()
            .Where(w => w.TenantId == tenantId);

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TenantWebhook>> GetActiveForEventAsync(Guid tenantId, TenantWebhookEventType eventType, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantWebhook>()
            .Where(w => w.TenantId == tenantId
                && w.IsActive
                && w.EventTypes.Contains(eventType.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantWebhook> CreateAsync(TenantWebhook webhook, CancellationToken cancellationToken = default)
    {
        _context.Set<TenantWebhook>().Add(webhook);
        await _context.SaveChangesAsync(cancellationToken);
        return webhook;
    }

    public async Task<TenantWebhook> UpdateAsync(TenantWebhook webhook, CancellationToken cancellationToken = default)
    {
        webhook.UpdatedAt = DateTime.UtcNow;
        _context.Set<TenantWebhook>().Update(webhook);
        await _context.SaveChangesAsync(cancellationToken);
        return webhook;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhook = await GetByIdAsync(id, cancellationToken);
        if (webhook == null)
            return false;

        webhook.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<TenantWebhook>> BulkCreateAsync(IEnumerable<TenantWebhook> webhooks, CancellationToken cancellationToken = default)
    {
        var webhookList = webhooks.ToList();
        _context.Set<TenantWebhook>().AddRange(webhookList);
        await _context.SaveChangesAsync(cancellationToken);
        return webhookList;
    }

    public async Task<(IEnumerable<TenantWebhookDelivery> Deliveries, int TotalCount)> GetDeliveriesAsync(
        Guid webhookId,
        bool? success = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TenantWebhookDelivery>()
            .Where(d => d.TenantWebhookId == webhookId);

        if (success.HasValue)
        {
            query = query.Where(d => d.Success == success.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(d => d.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(d => d.CreatedAt <= endDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var deliveries = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (deliveries, totalCount);
    }

    public async Task<(IEnumerable<TenantWebhookDelivery> Deliveries, int TotalCount)> GetFailedDeliveriesAsync(
        Guid tenantId,
        DateTime? sinceDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TenantWebhookDelivery>()
            .Include(d => d.TenantWebhook)
            .Where(d => d.TenantWebhook.TenantId == tenantId && !d.Success);

        if (sinceDate.HasValue)
        {
            query = query.Where(d => d.CreatedAt >= sinceDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var deliveries = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (deliveries, totalCount);
    }

    public async Task<TenantWebhookDelivery> RecordDeliveryAsync(TenantWebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        _context.Set<TenantWebhookDelivery>().Add(delivery);
        await _context.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    public async Task<TenantWebhookDelivery?> GetDeliveryByIdAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantWebhookDelivery>()
            .Include(d => d.TenantWebhook)
            .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);
    }
}
