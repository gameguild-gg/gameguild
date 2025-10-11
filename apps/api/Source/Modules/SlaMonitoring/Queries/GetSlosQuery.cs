using GameGuild.CQRS;
using GameGuild.Modules.SlaMonitoring.Entities;


namespace GameGuild.Modules.SlaMonitoring.Queries;

/// <summary>
/// Query to get all service level objectives with optional filtering.
/// </summary>
public record GetSlosQuery(
    Guid? TenantId = null,
    bool? IsActive = null,
    string? ServiceName = null,
    int Skip = 0,
    int Take = 50
) : IRequest<Result<IEnumerable<ServiceLevelObjective>>>;
