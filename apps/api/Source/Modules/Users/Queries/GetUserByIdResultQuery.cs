using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary>
/// Query to get a user by ID using Result<T> pattern
/// </summary>
public class GetUserByIdResultQuery : IResultQuery<User>
{
    public GetUserByIdResultQuery(int userId)
    {
        UserId = userId;
    }

    public int UserId { get; }
}
