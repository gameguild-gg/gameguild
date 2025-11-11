using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Commands;

public class RecordSliMetricCommandHandler(IServiceLevelObjectiveRepository sloRepository, IServiceLevelIndicatorRepository sliRepository, ISlaMonitoringService monitoringService)
    : ICommandHandler<RecordSliMetricCommand, SliMetricDto>
{
    public async Task<SliMetricDto> Handle(RecordSliMetricCommand request, CancellationToken cancellationToken)
    {
        // Verify SLO exists and belongs to tenant
        var slo = await sloRepository.GetByIdAsync(request.ServiceLevelObjectiveId, cancellationToken);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID '{request.ServiceLevelObjectiveId}' not found."); }

        if (slo.TenantId != request.TenantId) { throw new UnauthorizedAccessException("You do not have permission to record metrics for this SLO."); }

        // Create SLI metric
        ServiceLevelIndicator sli;

        if (request.IsSuccessful) { sli = ServiceLevelIndicator.CreateSuccess(request.ServiceLevelObjectiveId, request.Value, request.ResponseTimeMs, request.StatusCode, request.Endpoint); }
        else { sli = ServiceLevelIndicator.CreateFailure(request.ServiceLevelObjectiveId, request.Value, request.ErrorMessage ?? "Unknown error", request.ResponseTimeMs, request.StatusCode, request.Endpoint); }

        // Set Metadata and TenantId
        sli.Metadata = request.Metadata;
        sli.SetProperties(new Dictionary<string, object?> { { "TenantId", new TenantId(request.TenantId) } });

        await sliRepository.AddAsync(sli, cancellationToken);

        // Trigger SLO evaluation (async background process)
        _ = Task.Run(
            async () =>
            {
                try { await monitoringService.EvaluateSloAsync(request.ServiceLevelObjectiveId, CancellationToken.None); }
                catch
                {
                    // Log error but don't fail the request
                }
            },
            CancellationToken.None
        );

        return new SliMetricDto
        {
            ServiceLevelObjectiveId = sli.ServiceLevelObjectiveId,
            Timestamp = sli.Timestamp,
            Value = sli.Value,
            IsSuccessful = sli.IsSuccessful,
            ResponseTimeMs = sli.ResponseTimeMs,
            StatusCode = sli.StatusCode,
            Endpoint = sli.Endpoint,
            ErrorMessage = sli.ErrorMessage
        };
    }
}
