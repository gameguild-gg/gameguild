using GameGuild.CQRS;


namespace GameGuild.Modules.Tenants.Commands;

// Record Usage Command
public record RecordTenantUsageCommand(
    Guid TenantId,
    TenantUsageType UsageType,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    string Currency
) : IRequest<Result<TenantUsageRecord>>;

// Get Usage Records Query
public record GetTenantUsageRecordsQuery(
    Guid TenantId,
    DateTime PeriodStart,
    DateTime PeriodEnd
) : IRequest<Result<List<TenantUsageRecord>>>;

// Calculate Total Cost Query
public record CalculateTenantTotalCostQuery(
    Guid TenantId,
    DateTime PeriodStart,
    DateTime PeriodEnd
) : IRequest<Result<decimal>>;

// Create Billing Integration Command
public record CreateTenantBillingIntegrationCommand(
    Guid TenantId,
    TenantBillingProvider Provider,
    string Configuration,
    TenantBillingCycle BillingCycle,
    string Currency
) : IRequest<Result<TenantBillingIntegration>>;

// Update Billing Integration Command
public record UpdateTenantBillingIntegrationCommand(
    Guid IntegrationId,
    string Configuration
) : IRequest<Result<TenantBillingIntegration>>;

// Sync With Provider Command
public record SyncTenantBillingWithProviderCommand(
    Guid IntegrationId
) : IRequest<Result>;

// Get Billing Integration Query
public record GetTenantBillingIntegrationQuery(
    Guid TenantId
) : IRequest<Result<TenantBillingIntegration>>;
