using GameGuild.CQRS;
using MediatR;
using GameGuild.Core;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.SlaMonitoring.Commands;

/// <summary>
/// Command to update an existing service level objective.
/// </summary>
public record UpdateSloCommand(
    [Required] Guid SloId,
    string? Name = null,
    string? Description = null,
    string? ServiceName = null,
    [Range(0, 100)] double? TargetPercentage = null,
    [Range(1, 365)] int? TimeWindowDays = null,
    [Range(0, 100)] double? ErrorBudgetPercentage = null,
    [Range(0, 100)] double? AlertThresholdPercentage = null,
    bool? IsActive = null
) : IRequest<Result<Unit>>;
