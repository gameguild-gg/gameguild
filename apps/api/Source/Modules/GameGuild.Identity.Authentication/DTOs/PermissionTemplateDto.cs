namespace GameGuild.Identity.Authentication;

public class PermissionTemplateDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = new List<string>();

    public bool IsSystemTemplate { get; set; }

    public bool IsActive { get; set; }

    public string? MinimumTier { get; set; }

    public DateTime CreatedAt { get; set; }
}
