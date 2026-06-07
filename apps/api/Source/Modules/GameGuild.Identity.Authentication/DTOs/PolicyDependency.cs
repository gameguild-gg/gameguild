namespace GameGuild.Identity.Authentication;

public abstract class PolicyDependency
{
    public Guid PolicyId { get; set; }

    public string PolicyName { get; set; } = string.Empty;

    public List<Guid> DependsOnPolicyIds { get; set; } = new List<Guid>();

    public List<string> DependsOnPolicyNames { get; set; } = new List<string>();

    public string DependencyType { get; set; } = string.Empty;
}
