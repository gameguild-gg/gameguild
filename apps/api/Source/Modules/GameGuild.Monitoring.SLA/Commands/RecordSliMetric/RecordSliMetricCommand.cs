using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

public sealed record RecordSliMetricCommand(
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
