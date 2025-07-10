namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for auto-assignment request
/// </summary>
public class AutoAssignUsersDto {
  /// <summary>
  /// Optional tenant ID to limit auto-assignment to specific tenant
  /// </summary>
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Optional list of user emails to specifically auto-assign
  /// </summary>
  public List<string>? UserEmails { get; set; }
}
