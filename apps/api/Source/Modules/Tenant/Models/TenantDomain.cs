using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Tenant.Models;

/// <summary>
/// Represents a domain (top-level or subdomain) associated with a tenant for automatic user grouping.
/// Each tenant can have multiple domains with one designated as the main/principal domain.
/// Domains can be top-level (e.g., "champlain.edu") or subdomains (e.g., "student.champlain.edu").
/// </summary>
[Table("TenantDomains")]
[Index(nameof(TopLevelDomain), nameof(Subdomain), IsUnique = true)]
[Index(nameof(TenantId), nameof(IsMainDomain))]
public class TenantDomain : BaseEntity {
  private string _topLevelDomain = string.Empty;
  private string? _subdomain;
  private bool _isMainDomain = false;
  private bool _isSecondaryDomain = false;
  private Guid _tenantId;
  private Guid? _userGroupId;
  private Tenant? _tenant;
  private TenantUserGroup? _userGroup;

  /// <summary>
  /// The top-level domain name (e.g., "champlain.edu", "university.edu")
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string TopLevelDomain {
    get => _topLevelDomain;
    set => _topLevelDomain = value.ToLowerInvariant();
  }

  /// <summary>
  /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu", "faculty" for "faculty.champlain.edu")
  /// </summary>
  [MaxLength(100)]
  public string? Subdomain {
    get => _subdomain;
    set => _subdomain = value?.ToLowerInvariant();
  }

  /// <summary>
  /// Whether this is the main/principal domain for the tenant (only one per tenant)
  /// </summary>
  public bool IsMainDomain {
    get => _isMainDomain;
    set => _isMainDomain = value;
  }

  /// <summary>
  /// Whether this is a secondary domain for the tenant (can have multiple per tenant)
  /// </summary>
  public bool IsSecondaryDomain {
    get => _isSecondaryDomain;
    set => _isSecondaryDomain = value;
  }

  /// <summary>
  /// ID of the tenant this domain belongs to
  /// </summary>
  [Required]
  public Guid TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }

  /// <summary>
  /// ID of the user group that users with this domain should be automatically added to
  /// </summary>
  public Guid? UserGroupId {
    get => _userGroupId;
    set => _userGroupId = value;
  }

  /// <summary>
  /// Navigation property to the tenant
  /// </summary>
  [ForeignKey(nameof(TenantId))]
  public override Tenant? Tenant {
    get => _tenant;
    set => _tenant = value;
  }

  /// <summary>
  /// Navigation property to the user group
  /// </summary>
  [ForeignKey(nameof(UserGroupId))]
  public virtual TenantUserGroup? UserGroup {
    get => _userGroup;
    set => _userGroup = value;
  }

  /// <summary>
  /// Gets the full domain string including subdomain if present
  /// </summary>
  [NotMapped]
  public string FullDomainName => string.IsNullOrEmpty(Subdomain) ? TopLevelDomain : $"{Subdomain}.{TopLevelDomain}";

  /// <summary>
  /// Gets the domain type as a string for display purposes
  /// </summary>
  [NotMapped]
  public string DomainType {
    get {
      if (IsMainDomain) return "Main";
      if (IsSecondaryDomain) return "Secondary";

      return "Standard";
    }
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public TenantDomain() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial tenant domain data</param>
  public TenantDomain(object partial) : base(partial) { }

  /// <summary>
  /// Checks if an email matches this domain
  /// </summary>
  /// <param name="email">Email to check</param>
  /// <returns>True if the email matches this domain</returns>
  public bool MatchesEmail(string email) {
    if (string.IsNullOrEmpty(email) || !email.Contains('@')) return false;

    var emailDomain = email.Split('@')[1].ToLowerInvariant();

    return emailDomain == FullDomainName;
  }
}
