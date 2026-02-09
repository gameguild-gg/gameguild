using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to completely replace user metadata
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">The replacement request containing all fields</param>
public sealed record ReplaceUserMetadataCommand(Guid UserId, ReplaceUserMetadataRequest Request) : ICommand;
