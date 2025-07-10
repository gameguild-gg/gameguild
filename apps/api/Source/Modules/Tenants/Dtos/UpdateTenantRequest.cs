using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Request DTO for updating a tenant
/// </summary>
public class UpdateTenantRequest
{
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  [MaxLength(500)]
  public string? Description { get; set; }

  public bool IsActive { get; set; } = true;

  [Required]
  [MaxLength(255)]
  public string? Slug { get; set; }
}