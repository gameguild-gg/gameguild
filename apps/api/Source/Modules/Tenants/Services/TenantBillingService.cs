using GameGuild.Modules.Tenants;
using GameGuild.Modules.Tenants.Repositories;

namespace GameGuild.Modules.Tenants.Services;

public interface ITenantBillingService
{
    Task<TenantUsageRecord> RecordUsageAsync(Guid tenantId, TenantUsageType usageType, decimal quantity, string unit, decimal unitPrice, string currency);

    Task<List<TenantUsageRecord>> GetUsageRecordsAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task<decimal> CalculateTotalCostAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task<TenantBillingIntegration> CreateBillingIntegrationAsync(Guid tenantId, TenantBillingProvider provider, string configuration, TenantBillingCycle billingCycle, string currency);

    Task<TenantBillingIntegration> UpdateBillingIntegrationAsync(Guid integrationId, string configuration);

    Task SyncWithProviderAsync(Guid integrationId);
}

public class TenantBillingService : ITenantBillingService
{
    private readonly ITenantBillingRepository _repository;
    private readonly ILogger<TenantBillingService> _logger;

    public TenantBillingService(ITenantBillingRepository repository, ILogger<TenantBillingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TenantUsageRecord> RecordUsageAsync(Guid tenantId, TenantUsageType usageType, decimal quantity, string unit, decimal unitPrice, string currency)
    {
        var usageRecord = new TenantUsageRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UsageType = usageType,
            Quantity = quantity,
            Unit = unit,
            UnitPrice = unitPrice,
            Currency = currency,
            RecordedAt = DateTime.UtcNow,
            PeriodStart = DateTime.UtcNow.Date,
            PeriodEnd = DateTime.UtcNow.Date.AddDays(1),
            Metadata = "{}"
        };

        usageRecord.CalculateCost(quantity, unitPrice);

        await _repository.CreateUsageRecordAsync(usageRecord);

        _logger.LogInformation("Recorded usage for tenant {TenantId}: {UsageType} = {Quantity} {Unit} @ {UnitPrice} {Currency}",
            tenantId, usageType, quantity, unit, unitPrice, currency);

        return usageRecord;
    }

    public async Task<List<TenantUsageRecord>> GetUsageRecordsAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        return await _repository.GetUsageRecordsByPeriodAsync(tenantId, periodStart, periodEnd, cancellationToken);
    }

    public async Task<decimal> CalculateTotalCostAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        var records = await GetUsageRecordsAsync(tenantId, periodStart, periodEnd, cancellationToken);
        return records.Sum(r => r.TotalCost);
    }

    public async Task<TenantBillingIntegration> CreateBillingIntegrationAsync(Guid tenantId, TenantBillingProvider provider, string configuration, TenantBillingCycle billingCycle, string currency)
    {
        var integration = new TenantBillingIntegration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Provider = provider,
            IsEnabled = true,
            Configuration = configuration,
            BillingCycle = billingCycle,
            Currency = currency,
            Status = TenantBillingStatus.Active,
            Metadata = "{}"
        };

        await _repository.CreateBillingIntegrationAsync(integration);

        _logger.LogInformation("Created billing integration {IntegrationId} for tenant {TenantId} with provider {Provider}",
            integration.Id, tenantId, provider);

        return integration;
    }

    public async Task<TenantBillingIntegration> UpdateBillingIntegrationAsync(Guid integrationId, string configuration)
    {
        var integration = await _repository.GetBillingIntegrationByIdAsync(integrationId);
        if (integration == null)
        {
            throw new InvalidOperationException($"Billing integration {integrationId} not found");
        }

        integration.Configuration = configuration;
        await _repository.UpdateBillingIntegrationAsync(integration);

        _logger.LogInformation("Updated billing integration {IntegrationId}", integrationId);

        return integration;
    }

    public async Task SyncWithProviderAsync(Guid integrationId)
    {
        var integration = await _repository.GetBillingIntegrationByIdAsync(integrationId);
        if (integration == null)
        {
            throw new InvalidOperationException($"Billing integration {integrationId} not found");
        }

        integration.SyncWithProvider();
        await _repository.UpdateBillingIntegrationAsync(integration);

        _logger.LogInformation("Synced billing integration {IntegrationId} with provider", integrationId);
    }
}
