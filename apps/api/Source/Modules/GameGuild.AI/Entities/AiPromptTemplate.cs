namespace GameGuild.AI;

public sealed class AiPromptTemplate : EntityBase
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public string? SystemPrompt { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSystemTemplate { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
