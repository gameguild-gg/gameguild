using GameGuild.Database;
using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants.Repositories;

public class TenantBillingRepository : ITenantBillingRepository
{
    private readonly ApplicationDbContext _context;

    public TenantBillingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Usage Records
    public async Task<TenantUsageRecord> CreateUsageRecordAsync(TenantUsageRecord record, CancellationToken cancellationToken = default)
    {
        await _context.Set<TenantUsageRecord>().AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<TenantUsageRecord?> GetUsageRecordByIdAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantUsageRecord>()
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);
    }

    public async Task<List<TenantUsageRecord>> GetUsageRecordsByPeriodAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantUsageRecord>()
            .Where(r => r.TenantId == tenantId && r.RecordedAt >= periodStart && r.RecordedAt <= periodEnd)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TenantUsageRecord>> GetUsageRecordsByTypeAsync(Guid tenantId, TenantUsageType usageType, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantUsageRecord>()
            .Where(r => r.TenantId == tenantId && r.UsageType == usageType)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    // Billing Integrations
    public async Task<TenantBillingIntegration> CreateBillingIntegrationAsync(TenantBillingIntegration integration, CancellationToken cancellationToken = default)
    {
        await _context.Set<TenantBillingIntegration>().AddAsync(integration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return integration;
    }

    public async Task<TenantBillingIntegration> UpdateBillingIntegrationAsync(TenantBillingIntegration integration, CancellationToken cancellationToken = default)
    {
        _context.Set<TenantBillingIntegration>().Update(integration);
        await _context.SaveChangesAsync(cancellationToken);
        return integration;
    }

    public async Task<TenantBillingIntegration?> GetBillingIntegrationByIdAsync(Guid integrationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantBillingIntegration>()
            .FirstOrDefaultAsync(i => i.Id == integrationId, cancellationToken);
    }

    public async Task<TenantBillingIntegration?> GetBillingIntegrationByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantBillingIntegration>()
            .FirstOrDefaultAsync(i => i.TenantId == tenantId, cancellationToken);
    }

    public async Task DeleteBillingIntegrationAsync(Guid integrationId, CancellationToken cancellationToken = default)
    {
        var integration = await GetBillingIntegrationByIdAsync(integrationId, cancellationToken);
        if (integration != null)
        {
            _context.Set<TenantBillingIntegration>().Remove(integration);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
