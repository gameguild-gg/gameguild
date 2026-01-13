using GameGuild.CQRS;

namespace GameGuild.Social.Profiles;

/// <summary>
///     Query to get user profile by user ID
/// </summary>
/// <param name="UserId">The user ID to get profile for</param>
public record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto?>;
