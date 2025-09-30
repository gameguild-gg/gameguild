using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Query to get user profile using CQRS pattern </summary>
public class GetUserProfileQuery : IRequest<UserProfileDto>
{
    /// <summary> User ID </summary>
    public Guid UserId { get; set; }
}
