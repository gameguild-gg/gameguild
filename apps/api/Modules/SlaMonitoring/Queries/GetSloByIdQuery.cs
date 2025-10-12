using GameGuild.CQRS;
using GameGuild.Modules.SlaMonitoring.Entities;


namespace GameGuild.Modules.SlaMonitoring.Queries;

/// <summary>
/// Query to get a service level objective by ID.
/// </summary>
public record GetSloByIdQuery(
    Guid SloId
) : IRequest<Result<ServiceLevelObjective>>;
