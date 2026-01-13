using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to update dispute status
/// </summary>
public record UpdateDisputeStatusCommand(Guid DisputeId, DisputeStatus NewStatus, DateTime? DueDate = null) : ICommand;
