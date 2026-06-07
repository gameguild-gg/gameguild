namespace GameGuild.Identity.Authentication;

public abstract class ComplianceIssue
{
    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public int Count { get; set; }
}
