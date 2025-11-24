using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to resolve a dispute
/// </summary>
public record ResolveDisputeCommand(Guid DisputeId, DisputeResolution Resolution, string? Notes = null, Guid? ResolvedBy = null) : ICommand;
