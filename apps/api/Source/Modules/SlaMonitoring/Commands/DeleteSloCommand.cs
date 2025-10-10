using GameGuild.CQRS;
using MediatR;
using GameGuild.Core;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.SlaMonitoring.Commands;

/// <summary>
/// Command to delete a service level objective.
/// </summary>
public record DeleteSloCommand(
    [Required] Guid SloId
) : IRequest<Result<Unit>>;
