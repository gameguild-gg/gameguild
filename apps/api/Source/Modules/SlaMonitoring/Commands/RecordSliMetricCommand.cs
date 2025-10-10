using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Core;

namespace GameGuild.Modules.SlaMonitoring.Commands;

/// <summary>
/// Command to record a service level indicator metric.
/// </summary>
public record RecordSliMetricCommand(
    Guid SloId,
    double MetricValue,
    bool IsSuccessful,
    Dictionary<string, string>? Metadata = null
) : IRequest<Result<Unit>>;
