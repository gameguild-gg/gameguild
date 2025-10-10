using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Core;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.SlaMonitoring.Commands;

/// <summary>
/// Command to delete a service level objective.
/// </summary>
public record DeleteSloCommand(
    [Required] Guid SloId
) : IRequest<Result<Unit>>;
