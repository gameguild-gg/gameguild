using GameGuild.Common;
using GameGuild.Modules.UserProfiles.Entities;

namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Event raised when a user profile is created
/// </summary>
public sealed class UserProfileCreatedEvent(
    Guid userProfileId, 
    Guid userId,
    string displayName,
    string givenName,
    string familyName,
    DateTime createdAt) : DomainEventBase(userProfileId, nameof(UserProfile))
{
    public Guid UserProfileId { get; } = userProfileId;
    public Guid UserId { get; } = userId;
    public string DisplayName { get; } = displayName;
    public string GivenName { get; } = givenName;
    public string FamilyName { get; } = familyName;
    public DateTime CreatedAt { get; } = createdAt;
}
