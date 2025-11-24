using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Commands;

public record RecordSliMetricCommand(
    Guid TenantId,
    Guid ServiceLevelObjectiveId,
    bool IsSuccessful,
    double Value,
    long? ResponseTimeMs = null,
    int? StatusCode = null,
    string? Endpoint = null,
    string? Metadata = null,
    string? ErrorMessage = null
) : ICommand<SliMetricDto>;
