using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to update dispute status
/// </summary>
public record UpdateDisputeStatusCommand(Guid DisputeId, DisputeStatus NewStatus, DateTime? DueDate = null) : ICommand;
