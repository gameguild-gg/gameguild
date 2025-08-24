namespace GameGuild.Modules.UserProfiles;

public class UpdateUserProfileDto {
  [StringLength(100)] public string? GivenName { get; set; }

  [StringLength(100)] public string? FamilyName { get; set; }

  [StringLength(100)] public string? DisplayName { get; set; }

  [StringLength(200)] public string? Title { get; set; }

  [StringLength(1000)] public string? Description { get; set; }

  [StringLength(100)] public string? Slug { get; set; }

  public Guid? TenantId { get; set; }
}
