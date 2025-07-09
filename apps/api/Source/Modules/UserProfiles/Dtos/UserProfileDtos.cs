using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.UserProfiles.Dtos;

public class CreateUserProfileDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? GivenName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? FamilyName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? DisplayName { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Slug { get; set; }

    /// <summary>
    /// The user ID this profile belongs to (required for 1:1 relationship)
    /// </summary>
    [Required]
    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }
}

public class UpdateUserProfileDto {
  [StringLength(100)] public string? GivenName { get; set; }

  [StringLength(100)] public string? FamilyName { get; set; }

  [StringLength(100)] public string? DisplayName { get; set; }

  [StringLength(200)] public string? Title { get; set; }

  [StringLength(1000)] public string? Description { get; set; }

  [StringLength(100)] public string? Slug { get; set; }

  public Guid? TenantId { get; set; }
}

public class UserProfileResponseDto {
  public Guid Id { get; set; }

  public int Version { get; set; }

  public string? GivenName { get; set; }

  public string? FamilyName { get; set; }

  public string? DisplayName { get; set; }

  public string? Title { get; set; }

  public string? Description { get; set; }

  public string? Slug { get; set; }

  public Guid? TenantId { get; set; }

  public Guid? CreatedBy { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public DateTime? DeletedAt { get; set; }

  public bool IsDeleted { get; set; }
}
