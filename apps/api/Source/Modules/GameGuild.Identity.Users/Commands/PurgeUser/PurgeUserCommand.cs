using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Strategy for purging users
/// </summary>
public enum PurgeStrategy { Immediate, Scheduled, GracePeriod }

/// <summary>
///     Command to permanently delete a user (irreversible)
/// </summary>
/// <param name="UserId">The ID of the user to purge</param>
/// <param name="Strategy">The purge strategy to use</param>
public record PurgeUserCommand(Guid UserId, PurgeStrategy Strategy = PurgeStrategy.GracePeriod) : ICommand;
