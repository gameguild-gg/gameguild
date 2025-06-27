using MediatR;


namespace GameGuild.Modules.UserProfile.Commands;

/// <summary>
/// Command to update user profile with validation and business logic
/// </summary>
public class UpdateUserProfileCommand : IRequest<Models.UserProfile> {
  private Guid _userProfileId;

  private string? _givenName;

  private string? _familyName;

  private string? _displayName;

  public Guid UserProfileId {
    get => _userProfileId;
    set => _userProfileId = value;
  }

  public string? GivenName {
    get => _givenName;
    set => _givenName = value;
  }

  public string? FamilyName {
    get => _familyName;
    set => _familyName = value;
  }

  public string? DisplayName {
    get => _displayName;
    set => _displayName = value;
  }
}
