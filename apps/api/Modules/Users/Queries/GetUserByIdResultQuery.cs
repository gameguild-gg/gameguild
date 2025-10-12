using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Query to get a user by ID using Result<T> pattern </summary>
public class GetUserByIdResultQuery(Guid userId) : IResultQuery<User>
{
    public Guid UserId { get; } = userId;
}
