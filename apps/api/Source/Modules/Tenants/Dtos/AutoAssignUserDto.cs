using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for auto-assignment of a user
/// </summary>
public class AutoAssignUserDto {
  /// <summary>
  /// ID of the user to auto-assign
  /// </summary>
  [Required]
  public Guid UserId { get; set; }

  /// <summary>
  /// Email of the user to determine domain-based group assignment
  /// </summary>
  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;
}
