using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for creating a new tenant domain
/// </summary>
public class CreateTenantDomainDto {
  /// <summary>
  /// The top-level domain name (e.g., "champlain.edu", "university.edu")
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string TopLevelDomain { get; set; } = string.Empty;

  /// <summary>
  /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu")
  /// </summary>
  [MaxLength(100)]
  public string? Subdomain { get; set; }

  /// <summary>
  /// Whether this is the main/principal domain for the tenant (only one per tenant)
  /// </summary>
  public bool IsMainDomain { get; set; } = false;

  /// <summary>
  /// Whether this is a secondary domain for the tenant (can have multiple per tenant)
  /// </summary>
  public bool IsSecondaryDomain { get; set; } = false;

  /// <summary>
  /// ID of the tenant this domain belongs to
  /// </summary>
  [Required]
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the user group that users with this domain should be automatically added to
  /// </summary>
  public Guid? UserGroupId { get; set; }

  /// <summary>
  /// Convert DTO to domain model
  /// </summary>
  public TenantDomain ToTenantDomain() {
    return new TenantDomain {
      TopLevelDomain = TopLevelDomain,
      Subdomain = Subdomain,
      IsMainDomain = IsMainDomain,
      IsSecondaryDomain = IsSecondaryDomain,
      TenantId = TenantId,
      UserGroupId = UserGroupId,
    };
  }
}
