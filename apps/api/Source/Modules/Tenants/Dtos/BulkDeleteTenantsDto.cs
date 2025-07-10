using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for bulk deleting tenants
/// </summary>
public class BulkDeleteTenantsDto
{
    /// <summary>
    /// List of tenant IDs to delete
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one tenant ID must be provided")]
    public IEnumerable<Guid> TenantIds { get; set; } = new List<Guid>();
}
