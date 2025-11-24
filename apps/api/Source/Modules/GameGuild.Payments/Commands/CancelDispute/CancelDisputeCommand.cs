using GameGuild.CQRS;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to cancel a dispute
/// </summary>
public record CancelDisputeCommand(Guid DisputeId, string Reason) : ICommand;
