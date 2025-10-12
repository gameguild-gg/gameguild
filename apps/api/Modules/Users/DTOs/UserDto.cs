namespace GameGuild.Modules.Users;

/// <summary> DTO for user information </summary>
public class UserDto {
  /// <summary> User ID </summary>
  public Guid Id { get; set; }

  /// <summary> Username </summary>
  public string Username { get; set; } = string.Empty;

  /// <summary> Email address </summary>
  public string Email { get; set; } = string.Empty;

  /// <summary> Given name (first name) </summary>
  public string? GivenName { get; set; }

  /// <summary> Family name (last name) </summary>
  public string? FamilyName { get; set; }

  /// <summary> Creation timestamp </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary> Last update timestamp </summary>
  public DateTime? UpdatedAt { get; set; }
}
