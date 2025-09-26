namespace GameGuild.Modules.UserProfiles;

public class UserProfileResponse
{
    public Guid Id { get; set; }

    public int Version { get; set; }

    public string? DisplayName { get; set; }

    public Guid? TenantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }
}
