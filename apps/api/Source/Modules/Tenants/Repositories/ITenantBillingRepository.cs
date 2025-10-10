using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Tenants.Repositories;

public interface ITenantBillingRepository
{
    // Usage Records
    Task<TenantUsageRecord> CreateUsageRecordAsync(TenantUsageRecord record, CancellationToken cancellationToken = default);
    Task<TenantUsageRecord?> GetUsageRecordByIdAsync(Guid recordId, CancellationToken cancellationToken = default);
    Task<List<TenantUsageRecord>> GetUsageRecordsByPeriodAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);
    Task<List<TenantUsageRecord>> GetUsageRecordsByTypeAsync(Guid tenantId, TenantUsageType usageType, CancellationToken cancellationToken = default);

    // Billing Integrations
    Task<TenantBillingIntegration> CreateBillingIntegrationAsync(TenantBillingIntegration integration, CancellationToken cancellationToken = default);
    Task<TenantBillingIntegration> UpdateBillingIntegrationAsync(TenantBillingIntegration integration, CancellationToken cancellationToken = default);
    Task<TenantBillingIntegration?> GetBillingIntegrationByIdAsync(Guid integrationId, CancellationToken cancellationToken = default);
    Task<TenantBillingIntegration?> GetBillingIntegrationByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task DeleteBillingIntegrationAsync(Guid integrationId, CancellationToken cancellationToken = default);
}
