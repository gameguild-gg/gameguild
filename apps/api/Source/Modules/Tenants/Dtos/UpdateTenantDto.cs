using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for updating a tenant
/// </summary>
public class UpdateTenantDto
{
    /// <summary>
    /// Name of the tenant
    /// </summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// Description of the tenant
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Slug for the tenant (URL-friendly unique identifier)
    /// </summary>
    [MaxLength(100)]
    public string? Slug { get; set; }

    /// <summary>
    /// Whether this tenant is active
    /// </summary>
    public bool? IsActive { get; set; }
}
