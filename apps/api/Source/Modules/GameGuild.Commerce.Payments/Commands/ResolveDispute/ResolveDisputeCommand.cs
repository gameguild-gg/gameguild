using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to resolve a dispute
/// </summary>
public record ResolveDisputeCommand(Guid DisputeId, DisputeResolution Resolution, string? Notes = null, Guid? ResolvedBy = null) : ICommand;
