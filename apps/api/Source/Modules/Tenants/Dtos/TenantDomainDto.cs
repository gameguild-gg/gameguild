using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for tenant domain response
/// </summary>
public class TenantDomainDto {
  /// <summary>
  /// Domain ID
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// The top-level domain name (e.g., "champlain.edu", "university.edu")
  /// </summary>
  public string TopLevelDomain { get; set; } = string.Empty;

  /// <summary>
  /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu")
  /// </summary>
  public string? Subdomain { get; set; }

  /// <summary>
  /// Whether this is the main/principal domain for the tenant
  /// </summary>
  public bool IsMainDomain { get; set; }

  /// <summary>
  /// Whether this is a secondary domain for the tenant
  /// </summary>
  public bool IsSecondaryDomain { get; set; }

  /// <summary>
  /// ID of the tenant this domain belongs to
  /// </summary>
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the user group that users with this domain should be automatically added to
  /// </summary>
  public Guid? UserGroupId { get; set; }

  /// <summary>
  /// When the domain was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the domain was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Create from domain model
  /// </summary>
  public static TenantDomainDto FromTenantDomain(TenantDomain domain) {
    return new TenantDomainDto {
      Id = domain.Id,
      TopLevelDomain = domain.TopLevelDomain,
      Subdomain = domain.Subdomain,
      IsMainDomain = domain.IsMainDomain,
      IsSecondaryDomain = domain.IsSecondaryDomain,
      TenantId = domain.TenantId,
      UserGroupId = domain.UserGroupId,
      CreatedAt = domain.CreatedAt,
      UpdatedAt = domain.UpdatedAt,
    };
  }
}
