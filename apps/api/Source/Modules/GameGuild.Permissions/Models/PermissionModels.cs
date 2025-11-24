namespace GameGuild.Permissions.Domain.Models;

/// <summary>
///     Campaign statistics for access review
/// </summary>
public class CampaignStatistics
{
    public int TotalItems { get; set; }

    public int Reviewed { get; set; }

    public int Pending { get; set; }

    public int Approved { get; set; }

    public int Revoked { get; set; }

    public double CompletionPercentage { get; set; }
}

/// <summary>
///     Context for ABAC policy evaluation
/// </summary>
public class AbacEvaluationContext
{
    public Guid UserId { get; set; }

    public Dictionary<string, object> UserAttributes { get; set; } = new Dictionary<string, object>();

    public Guid? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public Dictionary<string, object> ResourceAttributes { get; set; } = new Dictionary<string, object>();

    public string Action { get; set; } = string.Empty;

    public Dictionary<string, object> EnvironmentAttributes { get; set; } = new Dictionary<string, object>();
}

/// <summary>
///     Result of ABAC policy evaluation
/// </summary>
public class AbacEvaluationResult
{
    public bool Allowed { get; set; }

    public List<Guid> MatchedPolicyIds { get; set; } = new List<Guid>();

    public string? DenyReason { get; set; }

    public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
}

/// <summary>
///     Permission usage metrics for analytics
/// </summary>
public class PermissionUsageMetrics
{
    public string PermissionType { get; set; } = string.Empty;

    public int UsageCount { get; set; }

    public int UniqueUsers { get; set; }

    public DateTime? LastUsed { get; set; }

    public Dictionary<string, int> UsageByResource { get; set; } = new Dictionary<string, int>();
}

/// <summary>
///     Permission trend data
/// </summary>
public class PermissionTrend
{
    public DateTime Date { get; set; }

    public string PermissionType { get; set; } = string.Empty;

    public int GrantCount { get; set; }

    public int RevokeCount { get; set; }

    public int ActiveCount { get; set; }
}

/// <summary>
///     User activity summary for analytics
/// </summary>
public class UserActivitySummary
{
    public Guid UserId { get; set; }

    public int TotalActions { get; set; }

    public DateTime? LastActivity { get; set; }

    public Dictionary<string, int> ActionsByPermission { get; set; } = new Dictionary<string, int>();

    public List<string> ResourcesAccessed { get; set; } = new List<string>();
}

/// <summary>
///     Resource access pattern for analytics
/// </summary>
public class ResourceAccessPattern
{
    public Guid ResourceId { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public int AccessCount { get; set; }

    public int UniqueUsers { get; set; }

    public Dictionary<string, int> AccessByPermission { get; set; } = new Dictionary<string, int>();

    public List<DateTime> PeakAccessTimes { get; set; } = new List<DateTime>();
}

/// <summary>
///     Anomaly detection result
/// </summary>
public class PermissionAnomaly
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string AnomalyType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public Guid? ResourceId { get; set; }

    public string? PermissionType { get; set; }

    public double Confidence { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
}

/// <summary>
///     Conditional policy evaluation request
/// </summary>
public class PolicyEvaluationRequest
{
    public Guid UserId { get; set; }

    public string PermissionType { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }

    public string? DeviceType { get; set; }

    public string? Location { get; set; }

    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
}

/// <summary>
///     Policy evaluation result
/// </summary>
public class PolicyEvaluationResult
{
    public bool Allowed { get; set; }

    public List<string> AppliedPolicies { get; set; } = new List<string>();

    public PolicyAction RequiredAction { get; set; }

    public string? DenyReason { get; set; }

    public bool Requires2FA { get; set; }

    public bool RequiresApproval { get; set; }
}

/// <summary>
///     Data masking result
/// </summary>
public class MaskingResult
{
    public string FieldName { get; set; } = string.Empty;

    public string OriginalValue { get; set; } = string.Empty;

    public string MaskedValue { get; set; } = string.Empty;

    public MaskingType MaskingType { get; set; }

    public bool WasMasked { get; set; }
}

/// <summary>
///     Permission analytics report
/// </summary>
public class PermissionAnalyticsReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public int TotalPermissionGrants { get; set; }

    public int TotalPermissionRevocations { get; set; }

    public int ActiveUsers { get; set; }

    public int ActiveResources { get; set; }

    public List<PermissionUsageMetrics> TopPermissions { get; set; } = new List<PermissionUsageMetrics>();

    public List<UserActivitySummary> MostActiveUsers { get; set; } = new List<UserActivitySummary>();

    public List<ResourceAccessPattern> MostAccessedResources { get; set; } = new List<ResourceAccessPattern>();

    public List<PermissionAnomaly> DetectedAnomalies { get; set; } = new List<PermissionAnomaly>();
}

// ==================== PERMISSION GRAPH MODELS ====================

/// <summary>
///     Represents a node in the permission graph
/// </summary>
public class PermissionGraphNode
{
    public Guid Id { get; set; }

    public string NodeType { get; set; } = string.Empty; // User, Template, Resource, Permission, Delegation

    public string Name { get; set; } = string.Empty;

    public Dictionary<string, object>? Attributes { get; set; }
}

/// <summary>
///     Represents an edge/relationship in the permission graph
/// </summary>
public class PermissionGraphEdge
{
    public Guid SourceId { get; set; }

    public Guid TargetId { get; set; }

    public string RelationType { get; set; } = string.Empty; // HasPermission, Inherits, Delegates, AppliesTo

    public Dictionary<string, object>? Properties { get; set; }

    public double Weight { get; set; } = 1.0;
}

/// <summary>
///     Complete permission graph with nodes and edges
/// </summary>
public class PermissionGraph
{
    public List<PermissionGraphNode> Nodes { get; set; } = new List<PermissionGraphNode>();

    public List<PermissionGraphEdge> Edges { get; set; } = new List<PermissionGraphEdge>();

    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
///     Impact analysis result for permission changes
/// </summary>
public class PermissionImpactAnalysis
{
    public Guid TargetEntityId { get; set; }

    public string TargetEntityType { get; set; } = string.Empty;

    public string[ ] AffectedPermissions { get; set; } = Array.Empty<string>();

    public List<ImpactedEntity> DirectlyImpacted { get; set; } = new List<ImpactedEntity>();

    public List<ImpactedEntity> IndirectlyImpacted { get; set; } = new List<ImpactedEntity>();

    public int TotalUsersAffected { get; set; }

    public int TotalResourcesAffected { get; set; }

    public int TotalDelegationsAffected { get; set; }

    public ImpactSeverity Severity { get; set; }

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Recommendations { get; set; } = new List<string>();

    public Dictionary<string, int> ImpactByType { get; set; } = new Dictionary<string, int>();

    public TimeSpan AnalysisDuration { get; set; }
}

/// <summary>
///     Entity impacted by permission change
/// </summary>
public class ImpactedEntity
{
    public Guid EntityId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string[ ] ImpactedPermissions { get; set; } = Array.Empty<string>();

    public string ImpactType { get; set; } = string.Empty; // Granted, Revoked, Modified, Inherited

    public int HopsFromSource { get; set; }

    public List<Guid> PathFromSource { get; set; } = new List<Guid>();
}

/// <summary>
///     Influential node in permission graph
/// </summary>
public class InfluentialNode
{
    public Guid NodeId { get; set; }

    public string NodeType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int IncomingEdges { get; set; }

    public int OutgoingEdges { get; set; }

    public int TotalConnections { get; set; }

    public double CentralityScore { get; set; }
}

/// <summary>
///     Permission visualization settings
/// </summary>
public class PermissionVisualizationSettings
{
    public int MaxDepth { get; set; } = 5;

    public bool IncludeInactiveNodes { get; set; } = false;

    public bool IncludeExpiredPermissions { get; set; } = false;

    public string[ ]? NodeTypesFilter { get; set; }

    public string[ ]? RelationTypesFilter { get; set; }

    public string LayoutAlgorithm { get; set; } = "ForceDirected"; // ForceDirected, Hierarchical, Circular

    public bool GroupByTenant { get; set; } = true;

    public bool ShowPermissionDetails { get; set; } = true;
}

// ==================== TEMPLATE VERSIONING MODELS ====================

/// <summary>
///     Version comparison result
/// </summary>
public class VersionDiff
{
    public int FromVersion { get; set; }

    public int ToVersion { get; set; }

    public string[ ] AddedPermissions { get; set; } = Array.Empty<string>();

    public string[ ] RemovedPermissions { get; set; } = Array.Empty<string>();

    public string[ ] UnchangedPermissions { get; set; } = Array.Empty<string>();

    public TemplateChangeType ChangeType { get; set; }

    public string? ChangeNotes { get; set; }
}

/// <summary>
///     Dry run result for migration testing
/// </summary>
public class DryRunResult
{
    public int AffectedUsers { get; set; }

    public int AffectedTenants { get; set; }

    public List<string> PermissionsToAdd { get; set; } = new List<string>();

    public List<string> PermissionsToRemove { get; set; } = new List<string>();

    public List<string> PotentialIssues { get; set; } = new List<string>();

    public List<string> Warnings { get; set; } = new List<string>();

    public TimeSpan EstimatedDuration { get; set; }

    public bool HasBreakingChanges { get; set; }
}

/// <summary>
///     Migration error details
/// </summary>
public class MigrationError
{
    public Guid EntityId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     Migration log entry
/// </summary>
public class MigrationLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Level { get; set; } = "Info"; // Info, Warning, Error

    public string Message { get; set; } = string.Empty;

    public Dictionary<string, object>? Data { get; set; }
}

// ==================== POLICY REGISTRY MODELS ====================

/// <summary>
///     Registry statistics
/// </summary>
public class RegistryStatistics
{
    public int TotalBundles { get; set; }

    public int ActiveBundles { get; set; }

    public int TotalDeployments { get; set; }

    public int ActiveDeployments { get; set; }

    public Dictionary<PolicyBundleType, int> BundlesByType { get; set; } = new Dictionary<PolicyBundleType, int>();

    public Dictionary<PolicyBundleStatus, int> BundlesByStatus { get; set; } = new Dictionary<PolicyBundleStatus, int>();
}
