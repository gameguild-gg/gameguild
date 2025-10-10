using GameGuild.Core.Common;
using GameGuild.Modules.Tenants.Abstractions;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants.Handlers;

/// <summary>
///     Handler for checking usage limits
/// </summary>
public class CheckUsageLimitsHandler : IRequestHandler<CheckUsageLimitsQuery, Result<UsageLimitsCheckDto>>
{
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<CheckUsageLimitsHandler> _logger;

    public CheckUsageLimitsHandler(
        IUsageTrackingService usageTrackingService,
        ITenantRepository tenantRepository,
        ILogger<CheckUsageLimitsHandler> logger)
    {
        _usageTrackingService = usageTrackingService;
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<Result<UsageLimitsCheckDto>> Handle(
        CheckUsageLimitsQuery request,
        CancellationToken cancellationToken)
    {
        // Check if tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", request.TenantId);
            return Result<UsageLimitsCheckDto>.Failure("Tenant not found");
        }

        List<ResourceLimitStatus> resourceStatuses = new();

        if (request.ResourceType.HasValue)
        {
            // Check specific resource
            var usage = await _usageTrackingService.GetUsageAsync(
                request.TenantId,
                request.ResourceType.Value,
                cancellationToken);

            if (usage != null)
            {
                resourceStatuses.Add(new ResourceLimitStatus(
                    usage.ResourceType,
                    usage.CustomResourceName,
                    usage.IsLimitExceeded,
                    usage.CurrentUsage,
                    usage.UsageLimit,
                    usage.UsagePercentage));
            }
        }
        else
        {
            // Check all resources
            var usageList = await _usageTrackingService.GetAllUsageAsync(request.TenantId, cancellationToken);

            resourceStatuses.AddRange(usageList.Select(usage => new ResourceLimitStatus(
                usage.ResourceType,
                usage.CustomResourceName,
                usage.IsLimitExceeded,
                usage.CurrentUsage,
                usage.UsageLimit,
                usage.UsagePercentage)));
        }

        var anyExceeded = resourceStatuses.Any(s => s.IsExceeded);

        if (anyExceeded)
        {
            var exceededResources = resourceStatuses.Where(s => s.IsExceeded).Select(s => s.ResourceType);
            _logger.LogWarning("Usage limits exceeded for tenant {TenantId}: {Resources}",
                request.TenantId, string.Join(", ", exceededResources));
        }

        var result = new UsageLimitsCheckDto(request.TenantId, anyExceeded, resourceStatuses);
        return Result<UsageLimitsCheckDto>.Success(result);
    }
}
