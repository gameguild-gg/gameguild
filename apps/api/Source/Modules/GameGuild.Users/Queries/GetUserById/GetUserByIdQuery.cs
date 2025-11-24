using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Queries;

/// <summary>
///     Query to get a user by their unique identifier
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record GetUserByIdQuery(Guid UserId) : IQuery<UserDto?>;
