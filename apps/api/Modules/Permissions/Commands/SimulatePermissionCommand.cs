using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to simulate permission evaluation without actually granting/denying access
/// What-if analysis for permission policies
/// </summary>
public class SimulatePermissionCommand : IRequest<PermissionSimulationResult>
{
    /// <summary>
    /// User ID to simulate for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Tenant ID context
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Resource ID to check (optional)
    /// </summary>
    public Guid? ResourceId { get; init; }

    /// <summary>
    /// Resource type to check
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Permission to simulate
    /// </summary>
    public PermissionType Permission { get; init; }

    /// <summary>
    /// Simulated user attributes for ABAC evaluation
    /// </summary>
    public Dictionary<string, object>? UserAttributes { get; init; }

    /// <summary>
    /// Simulated resource attributes for ABAC evaluation
    /// </summary>
    public Dictionary<string, object>? ResourceAttributes { get; init; }

    /// <summary>
    /// Simulated context attributes for ABAC evaluation
    /// </summary>
    public Dictionary<string, object>? ContextAttributes { get; init; }

    /// <summary>
    /// Include detailed trace of evaluation logic
    /// </summary>
    public bool IncludeDetailedTrace { get; init; } = true;
}

/// <summary>
/// Result of permission simulation
/// </summary>
public class PermissionSimulationResult
{
    /// <summary>
    /// Would access be granted?
    /// </summary>
    public bool WouldBeGranted { get; set; }

    /// <summary>
    /// Reason for the decision
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Layers evaluated (DAC, ABAC, RBAC)
    /// </summary>
    public List<LayerEvaluationResult> LayerResults { get; set; } = new();

    /// <summary>
    /// Total evaluation time in milliseconds
    /// </summary>
    public long EvaluationTimeMs { get; set; }

    /// <summary>
    /// Detailed evaluation trace
    /// </summary>
    public List<string> EvaluationTrace { get; set; } = new();

    /// <summary>
    /// Recommendations for granting access
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Result of evaluating a single authorization layer
/// </summary>
public class LayerEvaluationResult
{
    /// <summary>
    /// Layer name (DAC, ABAC, Owner Override, etc.)
    /// </summary>
    public string LayerName { get; set; } = string.Empty;

    /// <summary>
    /// Would this layer grant access?
    /// </summary>
    public bool WouldGrant { get; set; }

    /// <summary>
    /// Reason for the decision
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Matched policies or rules
    /// </summary>
    public List<string> MatchedRules { get; set; } = new();

    /// <summary>
    /// Evaluation time for this layer in milliseconds
    /// </summary>
    public long EvaluationTimeMs { get; set; }
}
