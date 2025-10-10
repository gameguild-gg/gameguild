using GameGuild.CQRS;
using GameGuild.Modules.Tenants.Commands;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Tenants.Repositories;
using GameGuild.Modules.Tenants.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants.Handlers;

// Record Usage Handler
public class RecordTenantUsageHandler : IRequestHandler<RecordTenantUsageCommand, Result<TenantUsageRecord>>
{
    private readonly ITenantBillingService _billingService;
    private readonly ILogger<RecordTenantUsageHandler> _logger;

    public RecordTenantUsageHandler(ITenantBillingService billingService, ILogger<RecordTenantUsageHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task<Result<TenantUsageRecord>> Handle(RecordTenantUsageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var record = await _billingService.RecordUsageAsync(
                request.TenantId, request.UsageType, request.Quantity,
                request.Unit, request.UnitPrice, request.Currency);

            return Result<TenantUsageRecord>.Success(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage for tenant {TenantId}", request.TenantId);
            return Result<TenantUsageRecord>.Failure($"Error recording usage: {ex.Message}");
        }
    }
}

// Get Usage Records Handler
public class GetTenantUsageRecordsHandler : IRequestHandler<GetTenantUsageRecordsQuery, Result<List<TenantUsageRecord>>>
{
    private readonly ITenantBillingService _billingService;
    private readonly ILogger<GetTenantUsageRecordsHandler> _logger;

    public GetTenantUsageRecordsHandler(ITenantBillingService billingService, ILogger<GetTenantUsageRecordsHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task<Result<List<TenantUsageRecord>>> Handle(GetTenantUsageRecordsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var records = await _billingService.GetUsageRecordsAsync(
                request.TenantId, request.PeriodStart, request.PeriodEnd, cancellationToken);

            return Result<List<TenantUsageRecord>>.Success(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage records for tenant {TenantId}", request.TenantId);
            return Result<List<TenantUsageRecord>>.Failure($"Error getting usage records: {ex.Message}");
        }
    }
}

// Calculate Total Cost Handler
public class CalculateTenantTotalCostHandler : IRequestHandler<CalculateTenantTotalCostQuery, Result<decimal>>
{
    private readonly ITenantBillingService _billingService;
    private readonly ILogger<CalculateTenantTotalCostHandler> _logger;

    public CalculateTenantTotalCostHandler(ITenantBillingService billingService, ILogger<CalculateTenantTotalCostHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task<Result<decimal>> Handle(CalculateTenantTotalCostQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var totalCost = await _billingService.CalculateTotalCostAsync(
                request.TenantId, request.PeriodStart, request.PeriodEnd, cancellationToken);

            return Result<decimal>.Success(totalCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total cost for tenant {TenantId}", request.TenantId);
            return Result<decimal>.Failure($"Error calculating total cost: {ex.Message}");
        }
    }
}

// Create Billing Integration Handler
public class CreateTenantBillingIntegrationHandler : IRequestHandler<CreateTenantBillingIntegrationCommand, Result<TenantBillingIntegration>>
{
    private readonly ITenantBillingService _billingService;
    private readonly ILogger<CreateTenantBillingIntegrationHandler> _logger;

    public CreateTenantBillingIntegrationHandler(ITenantBillingService billingService, ILogger<CreateTenantBillingIntegrationHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task<Result<TenantBillingIntegration>> Handle(CreateTenantBillingIntegrationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var integration = await _billingService.CreateBillingIntegrationAsync(
                request.TenantId, request.Provider, request.Configuration,
                request.BillingCycle, request.Currency);

            return Result<TenantBillingIntegration>.Success(integration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing integration for tenant {TenantId}", request.TenantId);
            return Result<TenantBillingIntegration>.Failure($"Error creating billing integration: {ex.Message}");
        }
    }
}

// Update Billing Integration Handler
public class UpdateTenantBillingIntegrationHandler : IRequestHandler<UpdateTenantBillingIntegrationCommand, Result<TenantBillingIntegration>>
{
    private readonly ITenantBillingService _billingService;
    private readonly ILogger<UpdateTenantBillingIntegrationHandler> _logger;

    public UpdateTenantBillingIntegrationHandler(ITenantBillingService billingService, ILogger<UpdateTenantBillingIntegrationHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task<Result<TenantBillingIntegration>> Handle(UpdateTenantBillingIntegrationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var integration = await _billingService.UpdateBillingIntegrationAsync(
                request.IntegrationId, request.Configuration);

            return Result<TenantBillingIntegration>.Success(integration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating billing integration {IntegrationId}", request.IntegrationId);
            return Result<TenantBillingIntegration>.Failure($"Error updating billing integration: {ex.Message}");
        }
    }
}

// Sync With Provider Handler
public class SyncTenantBillingWithProviderHandler : IRequestHandler<SyncTenantBillingWithProviderCommand, Result>
{
    private readonly ITenantBillingService _billingService;
    private readonly ILogger<SyncTenantBillingWithProviderHandler> _logger;

    public SyncTenantBillingWithProviderHandler(ITenantBillingService billingService, ILogger<SyncTenantBillingWithProviderHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task<Result> Handle(SyncTenantBillingWithProviderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _billingService.SyncWithProviderAsync(request.IntegrationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing billing integration {IntegrationId} with provider", request.IntegrationId);
            return Result.Failure($"Error syncing with provider: {ex.Message}");
        }
    }
}

// Get Billing Integration Handler
public class GetTenantBillingIntegrationHandler : IRequestHandler<GetTenantBillingIntegrationQuery, Result<TenantBillingIntegration>>
{
    private readonly ITenantBillingRepository _repository;
    private readonly ILogger<GetTenantBillingIntegrationHandler> _logger;

    public GetTenantBillingIntegrationHandler(ITenantBillingRepository repository, ILogger<GetTenantBillingIntegrationHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<TenantBillingIntegration>> Handle(GetTenantBillingIntegrationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var integration = await _repository.GetBillingIntegrationByTenantIdAsync(request.TenantId, cancellationToken);

            if (integration == null)
            {
                return Result<TenantBillingIntegration>.Failure($"No billing integration found for tenant {request.TenantId}");
            }

            return Result<TenantBillingIntegration>.Success(integration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing integration for tenant {TenantId}", request.TenantId);
            return Result<TenantBillingIntegration>.Failure($"Error getting billing integration: {ex.Message}");
        }
    }
}
