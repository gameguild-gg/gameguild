using System.Text.Json;
using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for tracking usage
/// </summary>
public class TrackUsageCommandHandler(ITenantRepository tenantRepository, IUsageTrackingService usageTrackingService) : ICommandHandler<TrackUsageCommand, TrackUsageResponse>
{
    public async Task<TrackUsageResponse> Handle(TrackUsageCommand request, CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);

        if (tenant == null) { return new TrackUsageResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found" }; }

        // Track the usage
        var usageTracking = new UsageTracking
        {
            TenantId = request.TenantId,
            ResourceType = request.ResourceType,
            Date = DateTime.UtcNow,
            UsageAmount = request.Quantity,
            Cost = request.Cost ?? 0m,
            Unit = request.ActionType,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
        };

        var trackingId = await usageTrackingService.TrackUsageAsync(usageTracking, cancellationToken);

        return new TrackUsageResponse { Success = true, Message = "Usage tracked successfully", TrackingId = trackingId };
    }
}
