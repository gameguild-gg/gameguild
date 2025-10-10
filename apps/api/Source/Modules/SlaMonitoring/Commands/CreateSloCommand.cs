using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Core;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.SlaMonitoring.Commands;

/// <summary>
/// Command to create a new service level objective.
/// </summary>
public record CreateSloCommand(
    [Required] string Name,
    string? Description,
    [Required] string ServiceName,
    [Range(0, 100)] double TargetPercentage,
    [Range(1, 365)] int TimeWindowDays = 30,
    [Range(0, 100)] double? ErrorBudgetPercentage = null,
    [Range(0, 100)] double AlertThresholdPercentage = 50.0,
    Guid? TenantId = null
) : IRequest<Result<Guid>>;
