using GameGuild.Modules.Tenants.Abstractions;
using GameGuild.Modules.Tenants.Commands;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants.Handlers;

/// <summary>
///     Handler for getting usage information
/// </summary>
public class GetUsageHandler : IRequestHandler<GetUsageQuery, Result<List<UsageTrackingDto>>>
{
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<GetUsageHandler> _logger;

    public GetUsageHandler(
        IUsageTrackingService usageTrackingService,
        ITenantRepository tenantRepository,
        ILogger<GetUsageHandler> logger)
    {
        _usageTrackingService = usageTrackingService;
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<Result<List<UsageTrackingDto>>> Handle(GetUsageQuery request, CancellationToken cancellationToken)
    {
        // Check if tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", request.TenantId);
            return Result<List<UsageTrackingDto>>.Failure("Tenant not found");
        }

        if (request.ResourceType.HasValue)
        {
            // Get specific resource usage
            var usage = await _usageTrackingService.GetUsageAsync(
                request.TenantId,
                request.ResourceType.Value,
                cancellationToken);

            if (usage == null)
            {
                return Result<List<UsageTrackingDto>>.Success(new List<UsageTrackingDto>());
            }

            var dto = new UsageTrackingDto(
                usage.Id,
                usage.TenantId,
                usage.ResourceType,
                usage.CustomResourceName,
                usage.CurrentUsage,
                usage.UsageLimit,
                usage.Unit,
                usage.IsLimitExceeded,
                usage.UsagePercentage,
                usage.RemainingCapacity,
                usage.LastUpdatedAt,
                usage.PeriodStartedAt);

            return Result<List<UsageTrackingDto>>.Success(new List<UsageTrackingDto> { dto });
        }
        else
        {
            // Get all usage for tenant
            var usageList = await _usageTrackingService.GetAllUsageAsync(request.TenantId, cancellationToken);

            var dtos = usageList.Select(usage => new UsageTrackingDto(
                usage.Id,
                usage.TenantId,
                usage.ResourceType,
                usage.CustomResourceName,
                usage.CurrentUsage,
                usage.UsageLimit,
                usage.Unit,
                usage.IsLimitExceeded,
                usage.UsagePercentage,
                usage.RemainingCapacity,
                usage.LastUpdatedAt,
                usage.PeriodStartedAt)).ToList();

            return Result<List<UsageTrackingDto>>.Success(dtos);
        }
    }
}
