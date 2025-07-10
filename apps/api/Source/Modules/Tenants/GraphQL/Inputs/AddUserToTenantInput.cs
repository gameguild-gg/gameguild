using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Input type for adding a user to a tenant
/// </summary>
public class AddUserToTenantInput {
  /// <summary>
  /// ID of the tenant
  /// </summary>
  [Required]
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the user
  /// </summary>
  [Required]
  public Guid UserId { get; set; }
}
