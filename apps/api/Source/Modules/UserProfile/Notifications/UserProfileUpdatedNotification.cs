using MediatR;


namespace GameGuild.Modules.UserProfile.Notifications;

/// <summary>
/// Notification sent when a user profile is updated
/// </summary>
public class UserProfileUpdatedNotification : INotification {
  private Guid _userProfileId;

  private Guid _userId;

  private DateTime _updatedAt = DateTime.UtcNow;

  private Dictionary<string, object> _changes = new();

  public Guid UserProfileId {
    get => _userProfileId;
    set => _userProfileId = value;
  }

  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  public DateTime UpdatedAt {
    get => _updatedAt;
    set => _updatedAt = value;
  }

  public Dictionary<string, object> Changes {
    get => _changes;
    set => _changes = value;
  }
}
