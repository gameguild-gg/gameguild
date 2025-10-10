using GameGuild.CQRS;
using GameGuild.Messaging;
using GameGuild.Modules.Resources.Commands;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Repositories;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Handlers;

/// <summary>
/// Handler for getting usage records
/// </summary>
public class GetUsageRecordsHandler : IRequestHandler<GetUsageRecordsQuery, Result<IEnumerable<ResourceUsageRecord>>>
{
    private readonly IResourceUsageRepository _usageRepository;
    private readonly ILogger<GetUsageRecordsHandler> _logger;

    public GetUsageRecordsHandler(IResourceUsageRepository usageRepository, ILogger<GetUsageRecordsHandler> logger)
    {
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ResourceUsageRecord>>> Handle(GetUsageRecordsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var records = await _usageRepository.GetUsageRecordsAsync(
                request.TenantId,
                request.UsageType,
                request.StartDate,
                request.EndDate);

            return Result<IEnumerable<ResourceUsageRecord>>.Success(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage records for tenant {TenantId}", request.TenantId);
            return Result<IEnumerable<ResourceUsageRecord>>.Failure("Failed to retrieve usage records");
        }
    }
}

/// <summary>
/// Handler for getting current usage summary
/// </summary>
public class GetCurrentUsageSummaryHandler : IRequestHandler<GetCurrentUsageSummaryQuery, Result<Dictionary<ResourceUsageType, long>>>
{
    private readonly IResourceUsageRepository _usageRepository;
    private readonly ILogger<GetCurrentUsageSummaryHandler> _logger;

    public GetCurrentUsageSummaryHandler(IResourceUsageRepository usageRepository, ILogger<GetCurrentUsageSummaryHandler> logger)
    {
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<Result<Dictionary<ResourceUsageType, long>>> Handle(GetCurrentUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _usageRepository.GetCurrentUsageSummaryAsync(request.TenantId);
            return Result<Dictionary<ResourceUsageType, long>>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage summary for tenant {TenantId}", request.TenantId);
            return Result<Dictionary<ResourceUsageType, long>>.Failure("Failed to retrieve usage summary");
        }
    }
}

/// <summary>
/// Handler for checking usage limits
/// </summary>
public class CheckUsageLimitsHandler : IRequestHandler<CheckUsageLimitsQuery, Result<Dictionary<ResourceUsageType, ResourceQuotaStatus>>>
{
    private readonly IResourceQuotaRepository _quotaRepository;
    private readonly IResourceUsageRepository _usageRepository;
    private readonly ILogger<CheckUsageLimitsHandler> _logger;

    public CheckUsageLimitsHandler(IResourceQuotaRepository quotaRepository, IResourceUsageRepository usageRepository, ILogger<CheckUsageLimitsHandler> logger)
    {
        _quotaRepository = quotaRepository;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<Result<Dictionary<ResourceUsageType, ResourceQuotaStatus>>> Handle(CheckUsageLimitsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var quotas = await _quotaRepository.GetQuotasByTenantIdAsync(request.TenantId, request.UsageType);
            var currentUsage = await _usageRepository.GetCurrentUsageSummaryAsync(request.TenantId);

            var result = new Dictionary<ResourceUsageType, ResourceQuotaStatus>();

            foreach (var quota in quotas)
            {
                var usage = currentUsage.GetValueOrDefault(quota.Type, 0);
                var percentageUsed = quota.HardLimit.HasValue && quota.HardLimit > 0
                    ? (double)usage / quota.HardLimit.Value * 100
                    : 0;

                var isWithinLimits = !quota.HardLimit.HasValue || usage <= quota.HardLimit.Value;

                result[quota.Type] = new ResourceQuotaStatus(
                    usage,
                    quota.SoftLimit,
                    quota.HardLimit,
                    isWithinLimits,
                    percentageUsed);
            }

            return Result<Dictionary<ResourceUsageType, ResourceQuotaStatus>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check usage limits for tenant {TenantId}", request.TenantId);
            return Result<Dictionary<ResourceUsageType, ResourceQuotaStatus>>.Failure("Failed to check usage limits");
        }
    }
}

/// <summary>
/// Handler for recording usage
/// </summary>
public class RecordUsageHandler : IRequestHandler<RecordUsageCommand, Result<ResourceUsageRecord>>
{
    private readonly IResourceUsageRepository _usageRepository;
    private readonly IResourceQuotaRepository _quotaRepository;
    private readonly ILogger<RecordUsageHandler> _logger;

    public RecordUsageHandler(IResourceUsageRepository usageRepository, IResourceQuotaRepository quotaRepository, ILogger<RecordUsageHandler> logger)
    {
        _usageRepository = usageRepository;
        _quotaRepository = quotaRepository;
        _logger = logger;
    }

    public async Task<Result<ResourceUsageRecord>> Handle(RecordUsageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if tenant has quota for this usage type
            var quota = await _quotaRepository.GetQuotaAsync(request.TenantId, request.UsageType);
            if (quota != null && quota.IsActive)
            {
                // Check if recording this usage would exceed hard limit
                if (quota.HardLimit.HasValue)
                {
                    var newUsage = quota.CurrentUsage + request.Count;
                    if (newUsage > quota.HardLimit.Value)
                    {
                        return Result<ResourceUsageRecord>.Failure($"Recording usage would exceed hard limit of {quota.HardLimit.Value}");
                    }
                }

                // Update quota current usage
                quota.CurrentUsage += request.Count;
                await _quotaRepository.UpdateAsync(quota);
            }

            // Create usage record
            var usageRecord = new ResourceUsageRecord
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Type = request.UsageType,
                Count = request.Count,
                PeriodStart = DateTime.UtcNow.Date,
                PeriodEnd = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1),
                Source = request.Source,
                UserId = request.UserId,
                ResourceId = request.ResourceId,
                Metadata = request.Metadata,
                CreatedAt = DateTime.UtcNow
            };

            await _usageRepository.AddAsync(usageRecord);
            return Result<ResourceUsageRecord>.Success(usageRecord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for tenant {TenantId}", request.TenantId);
            return Result<ResourceUsageRecord>.Failure("Failed to record usage");
        }
    }
}