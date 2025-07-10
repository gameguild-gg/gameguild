using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for updating an existing tenant domain
/// </summary>
public class UpdateTenantDomainDto {
  /// <summary>
  /// The top-level domain name (e.g., "champlain.edu", "university.edu")
  /// </summary>
  [MaxLength(255)]
  public string? TopLevelDomain { get; set; }

  /// <summary>
  /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu")
  /// </summary>
  [MaxLength(100)]
  public string? Subdomain { get; set; }

  /// <summary>
  /// Whether this is the main/principal domain for the tenant
  /// </summary>
  public bool? IsMainDomain { get; set; }

  /// <summary>
  /// Whether this is a secondary domain for the tenant
  /// </summary>
  public bool? IsSecondaryDomain { get; set; }

  /// <summary>
  /// ID of the user group that users with this domain should be automatically added to
  /// </summary>
  public Guid? UserGroupId { get; set; }

  /// <summary>
  /// Apply updates to domain model
  /// </summary>
  public void UpdateTenantDomain(TenantDomain domain) {
    if (!string.IsNullOrEmpty(TopLevelDomain)) domain.TopLevelDomain = TopLevelDomain;

    if (Subdomain != null) domain.Subdomain = Subdomain;

    if (IsMainDomain.HasValue) domain.IsMainDomain = IsMainDomain.Value;

    if (IsSecondaryDomain.HasValue) domain.IsSecondaryDomain = IsSecondaryDomain.Value;

    if (UserGroupId.HasValue) domain.UserGroupId = UserGroupId.Value;
  }
}

