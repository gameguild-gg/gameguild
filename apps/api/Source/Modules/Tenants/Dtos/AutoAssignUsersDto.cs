using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for auto-assigning multiple users to groups based on domain
/// </summary>
public class AutoAssignUsersDto {
  /// <summary>
  /// List of user IDs to auto-assign
  /// </summary>
  [MinLength(1, ErrorMessage = "At least one user ID must be provided")]
  public IEnumerable<Guid>? UserIds { get; set; }

  /// <summary>
  /// List of user emails to auto-assign
  /// </summary>
  [MinLength(1, ErrorMessage = "At least one user email must be provided")]
  public ICollection<string>? UserEmails { get; set; }

  /// <summary>
  /// ID of the tenant to auto-assign the users to
  /// </summary>
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Whether to force reassignment even if users are already assigned
  /// </summary>
  public bool ForceReassignment { get; set; } = false;
}
