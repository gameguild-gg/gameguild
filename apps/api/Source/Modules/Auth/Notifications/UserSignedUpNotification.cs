using MediatR;

namespace GameGuild.Modules.Auth.Notifications;

/// <summary>
/// Notification sent when a user successfully signs up
/// </summary>
public class UserSignedUpNotification : INotification {
  private Guid _userId;

  private string _email = string.Empty;

  private string _username = string.Empty;

  private DateTime _signUpTime = DateTime.UtcNow;

  private Guid? _tenantId;

  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  public string Email {
    get => _email;
    set => _email = value;
  }

  public string Username {
    get => _username;
    set => _username = value;
  }

  public DateTime SignUpTime {
    get => _signUpTime;
    set => _signUpTime = value;
  }

  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }
}
