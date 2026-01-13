using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to cancel a dispute
/// </summary>
public record CancelDisputeCommand(Guid DisputeId, string Reason) : ICommand;
