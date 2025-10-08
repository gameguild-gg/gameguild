namespace GameGuild.Modules.Permissions;

/// <summary>
/// Context for ABAC policy evaluation
/// Contains user, resource, and environmental attributes
/// </summary>
public class AbacEvaluationContext
{
    /// <summary>
    /// User attributes (role, department, clearance level, etc.)
    /// </summary>
    public Dictionary<string, object> UserAttributes { get; set; } = new();

    /// <summary>
    /// Resource attributes (type, owner, status, sensitivity, etc.)
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();

    /// <summary>
    /// Environmental/contextual attributes (time, IP address, location, etc.)
    /// </summary>
    public Dictionary<string, object> ContextAttributes { get; set; } = new();

    /// <summary>
    /// User ID for policy evaluation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant ID for policy evaluation
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Resource ID being accessed
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Resource type being accessed
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Permission being requested
    /// </summary>
    public PermissionType Permission { get; set; }

    /// <summary>
    /// Timestamp of the evaluation request
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of ABAC policy evaluation
/// </summary>
public class AbacEvaluationResult
{
    /// <summary>
    /// Whether access is granted
    /// </summary>
    public bool IsGranted { get; set; }

    /// <summary>
    /// Effect that determined the result (Allow or Deny)
    /// </summary>
    public PolicyEffect Effect { get; set; }

    /// <summary>
    /// Policy that determined the result
    /// </summary>
    public AbacPolicy? MatchedPolicy { get; set; }

    /// <summary>
    /// All matching policies evaluated
    /// </summary>
    public List<AbacPolicy> EvaluatedPolicies { get; set; } = new();

    /// <summary>
    /// Reason for the decision
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Evaluation duration in milliseconds
    /// </summary>
    public long EvaluationDurationMs { get; set; }

    /// <summary>
    /// Detailed evaluation trace for debugging
    /// </summary>
    public List<string> EvaluationTrace { get; set; } = new();
}
