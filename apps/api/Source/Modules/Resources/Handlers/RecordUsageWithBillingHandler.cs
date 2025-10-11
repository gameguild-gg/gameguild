using GameGuild.Modules.Resources.Commands;
using GameGuild.Modules.Resources.Events;
using GameGuild.Modules.Resources.Repositories;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Handlers;

/// <summary>
/// Handler for recording resource usage with billing event integration
/// </summary>
public class RecordUsageWithBillingHandler : IRequestHandler<RecordUsageCommand, Result<ResourceUsageRecord>> {
    private readonly IResourceUsageRepository _usageRepository;
    private readonly IResourceQuotaRepository _quotaRepository;
    private readonly IOutboxEventPublisher _eventPublisher;
    private readonly ILogger<RecordUsageWithBillingHandler> _logger;

    public RecordUsageWithBillingHandler(
        IResourceUsageRepository usageRepository,
        IResourceQuotaRepository quotaRepository,
        IOutboxEventPublisher eventPublisher,
        ILogger<RecordUsageWithBillingHandler> logger) {
        _usageRepository = usageRepository;
        _quotaRepository = quotaRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<ResourceUsageRecord>> Handle(RecordUsageCommand request, CancellationToken cancellationToken) {
        // Get current quota
        var quota = await _quotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.Type, cancellationToken);
        if (quota == null) {
            _logger.LogWarning("No quota found for tenant {TenantId} and type {Type}", request.TenantId, request.Type);
            return Result<ResourceUsageRecord>.Failure("Quota not found");
        }

        // Check if adding would exceed hard limit
        var wouldExceed = quota.HardLimit.HasValue &&
                          quota.CurrentUsage + request.Count > quota.HardLimit.Value;

        if (wouldExceed && !request.AllowOverage) {
            // Publish hard limit exceeded event
            await _eventPublisher.PublishAsync(new QuotaHardLimitExceededEvent {
                QuotaId = quota.Id,
                TenantId = request.TenantId,
                UsageType = request.Type,
                CurrentUsage = quota.CurrentUsage,
                HardLimit = quota.HardLimit!.Value,
                ExceededAt = DateTime.UtcNow,
                BlockedOperation = request.Source
            }, cancellationToken);

            _logger.LogWarning("Hard limit exceeded for tenant {TenantId}, type {Type}",
                request.TenantId, request.Type);

            return Result<ResourceUsageRecord>.Failure("Hard limit exceeded");
        }

        // Record usage
        var record = new ResourceUsageRecord {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Type = request.Type,
            Count = request.Count,
            Source = request.Source,
            UserId = request.UserId,
            ResourceId = request.ResourceId,
            Metadata = request.Metadata,
            RecordedAt = DateTime.UtcNow
        };

        await _usageRepository.AddAsync(record, cancellationToken);

        // Update quota
        quota.CurrentUsage += request.Count;
        await _quotaRepository.UpdateAsync(quota, cancellationToken);

        // Publish usage recorded event for billing integration
        await _eventPublisher.PublishAsync(new UsageRecordedEvent {
            RecordId = record.Id,
            TenantId = request.TenantId,
            Type = request.Type,
            Count = request.Count,
            Source = request.Source,
            UserId = request.UserId,
            ResourceId = request.ResourceId,
            Metadata = request.Metadata,
            RecordedAt = record.RecordedAt,
            CumulativeUsage = quota.CurrentUsage,
            RemainingQuota = quota.HardLimit - quota.CurrentUsage,
            IsOverLimit = quota.HardLimit.HasValue && quota.CurrentUsage > quota.HardLimit.Value
        }, cancellationToken);

        _logger.LogInformation("Recorded usage for tenant {TenantId}, type {Type}, count {Count}",
            request.TenantId, request.Type, request.Count);

        var dto = new ResourceUsageRecordDto {
            Id = record.Id,
            TenantId = record.TenantId,
            Type = record.Type,
            Count = record.Count,
            Source = record.Source,
            UserId = record.UserId,
            ResourceId = record.ResourceId,
            RecordedAt = record.RecordedAt
        };

        return Result<ResourceUsageRecord>.Success(dto);
    }
}
