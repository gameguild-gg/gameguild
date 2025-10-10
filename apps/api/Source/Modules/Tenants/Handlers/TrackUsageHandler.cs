using GameGuild.Core.Common;
using GameGuild.Modules.Tenants.Abstractions;
using GameGuild.Modules.Tenants;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants.Handlers;

/// <summary>
///     Handler for tracking resource usage
/// </summary>
public class TrackUsageHandler : IRequestHandler<TrackUsageCommand, Result<UsageTrackingDto>>
{
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TrackUsageHandler> _logger;

    public TrackUsageHandler(
        IUsageTrackingService usageTrackingService,
        ITenantRepository tenantRepository,
        ILogger<TrackUsageHandler> logger)
    {
        _usageTrackingService = usageTrackingService;
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<Result<UsageTrackingDto>> Handle(TrackUsageCommand request, CancellationToken cancellationToken)
    {
        // Check if tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found for usage tracking", request.TenantId);
            return Result<UsageTrackingDto>.Failure("Tenant not found");
        }

        // Track the usage
        var usage = await _usageTrackingService.TrackUsageAsync(
            request.TenantId,
            request.ResourceType,
            request.Amount,
            request.CustomResourceName,
            cancellationToken);

        // Check if limit exceeded
        if (usage.IsLimitExceeded)
        {
            _logger.LogWarning("Usage limit exceeded for tenant {TenantId}, resource {ResourceType}: {CurrentUsage}/{UsageLimit}",
                request.TenantId, request.ResourceType, usage.CurrentUsage, usage.UsageLimit);
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

        return Result<UsageTrackingDto>.Success(dto);
    }
}
