using GameGuild.CQRS;
using GameGuild.Core;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.SlaMonitoring.Commands;

/// <summary>
/// Command to resolve an SLO violation.
/// </summary>
public record ResolveSloViolationCommand(
    [Required] Guid ViolationId,
    string? ResolutionNotes = null
) : IRequest<Result<Unit>>;
