using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;

namespace GameGuild.Users.Queries.GetUserProfile;

/// <summary>
/// Query handler for getting user profile
/// </summary>
public class GetUserProfileQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserProfileQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        
        if (user == null)
            return null;

        // Return basic profile from user entity
        return new UserProfileDto(
            Id: Guid.NewGuid(), // Placeholder ID since profile is generated on-the-fly
            UserId: user.Id,
            DisplayName: user.Name,
            Bio: null,
            Location: null,
            Website: null,
            AvatarUrl: null,
            BannerUrl: null,
            TimeZone: null,
            Language: null,
            ProfileVisibility: "public",
            ShowEmail: false,
            ShowLocation: false,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt,
            Version: new byte[] { 1 }
        );
    }
}
