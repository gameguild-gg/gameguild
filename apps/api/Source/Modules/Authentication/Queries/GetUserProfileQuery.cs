using MediatR;

namespace GameGuild.Modules.Auth;

/// <summary>
/// Query to get current user profile using CQRS pattern
/// </summary>
public class GetUserProfileQuery : IRequest<UserProfileDto>
{
    /// <summary>
    /// User ID - will be extracted from the JWT claims
    /// </summary>
    public Guid UserId { get; set; }
}
