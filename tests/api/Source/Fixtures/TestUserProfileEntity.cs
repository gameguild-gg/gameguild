namespace GameGuild.Tests.Fixtures;

public class TestUserProfileEntity {
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for test only (not used by EF Core)
    public TestUserEntity? User { get; set; }
}
