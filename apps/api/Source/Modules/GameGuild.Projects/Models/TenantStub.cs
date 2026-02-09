using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Projects;

/// <summary> Stub for Tenant entity - PLANNED: Use from Tenants module when available </summary>
[Table("Tenants")]
public class Tenant : EntityBase {
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Slug { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}
