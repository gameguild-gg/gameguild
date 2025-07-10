using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for updating an existing tenant domain
/// </summary>
public class UpdateTenantDomainDto
{
    /// <summary>
    /// The top-level domain name (e.g., "champlain.edu")
    /// </summary>
    [MaxLength(255)]
    public string? TopLevelDomain { get; set; }

    /// <summary>
    /// Optional subdomain prefix (e.g., "student" for "student.champlain.edu")
    /// </summary>
    [MaxLength(100)]
    public string? Subdomain { get; set; }

    /// <summary>
    /// Whether this is the main domain for the tenant
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
}
