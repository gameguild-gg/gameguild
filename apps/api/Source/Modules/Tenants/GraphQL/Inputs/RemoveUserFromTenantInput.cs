using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Input type for removing a user from a tenant
/// </summary>
public class RemoveUserFromTenantInput {
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
