using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query to get a user by their unique identifier
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record GetUserByIdQuery(Guid UserId) : IQuery<UserDto?>;
