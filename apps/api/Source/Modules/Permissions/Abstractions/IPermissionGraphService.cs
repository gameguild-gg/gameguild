using GameGuild.Modules.Permissions.Constants;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Models;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for graph-based permission visualization and impact analysis
/// </summary>
public interface IPermissionGraphService
{
    /// <summary>
    /// Build a permission graph for a tenant
    /// </summary>
    Task<PermissionGraph> BuildTenantGraphAsync(Guid tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Build a permission graph for a specific user
    /// </summary>
    Task<PermissionGraph> BuildUserGraphAsync(Guid userId, Guid tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Build a permission graph for a specific resource
    /// </summary>
    Task<PermissionGraph> BuildResourceGraphAsync(Guid resourceId, string resourceType, Guid tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze the impact of granting a permission
    /// </summary>
    Task<PermissionImpactAnalysis> AnalyzeGrantImpactAsync(Guid userId, Guid tenantId, PermissionType[] permissions, string? scopeType = null, Guid? scopeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze the impact of revoking a permission
    /// </summary>
    Task<PermissionImpactAnalysis> AnalyzeRevokeImpactAsync(Guid userId, Guid tenantId, PermissionType[] permissions, string? scopeType = null, Guid? scopeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze the impact of modifying a delegation
    /// </summary>
    Task<PermissionImpactAnalysis> AnalyzeDelegationImpactAsync(Guid delegationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find all paths between two nodes in the permission graph
    /// </summary>
    Task<List<List<PermissionGraphNode>>> FindPathsAsync(Guid sourceId, Guid targetId, Guid tenantId, int maxDepth = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find circular dependencies in permission graph
    /// </summary>
    Task<List<List<Guid>>> FindCircularDependenciesAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most influential nodes (users/resources with most connections)
    /// </summary>
    Task<List<InfluentialNode>> GetInfluentialNodesAsync(Guid tenantId, int topN = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export graph to various formats
    /// </summary>
    Task<string> ExportGraphAsync(Guid tenantId, GraphExportFormat format, CancellationToken cancellationToken = default);
}

/// <summary>
/// An influential node in the permission graph
/// </summary>
public class InfluentialNode
{
    public Guid NodeId { get; set; }
    public string NodeType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int IncomingEdges { get; set; }
    public int OutgoingEdges { get; set; }
    public int TotalConnections { get; set; }
    public double CentralityScore { get; set; }
}

/// <summary>
/// Graph export formats
/// </summary>
public enum GraphExportFormat
{
    JSON = 0,
    GraphML = 1,
    DOT = 2,
    Cypher = 3,
    CSV = 4
}
