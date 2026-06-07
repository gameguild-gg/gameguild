namespace GameGuild.Identity.Authentication;

public abstract class PolicyConflict
{
    public Guid PolicyId1 { get; set; }

    public string PolicyName1 { get; set; } = string.Empty;

    public Guid PolicyId2 { get; set; }

    public string PolicyName2 { get; set; } = string.Empty;

    public string ConflictType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;
}
