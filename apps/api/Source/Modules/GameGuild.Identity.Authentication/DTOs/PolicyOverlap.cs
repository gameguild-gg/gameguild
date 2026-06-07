namespace GameGuild.Identity.Authentication;

public abstract class PolicyOverlap
{
    public List<Guid> PolicyIds { get; set; } = new List<Guid>();

    public List<string> PolicyNames { get; set; } = new List<string>();

    public string OverlapArea { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double OverlapPercentage { get; set; }
}
