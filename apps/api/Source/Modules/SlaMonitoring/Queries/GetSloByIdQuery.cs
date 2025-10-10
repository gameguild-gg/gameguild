using MediatR;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Queries;

/// <summary>
/// Query to get a service level objective by ID.
/// </summary>
public record GetSloByIdQuery(
    Guid SloId
) : IRequest<Result<ServiceLevelObjective>>;
