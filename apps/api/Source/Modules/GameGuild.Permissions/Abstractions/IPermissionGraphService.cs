using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Service interface for graph-based permission visualization and impact analysis.
///     Provides methods to build permission graphs, analyze impacts, and detect anomalies.
/// </summary>
public interface IPermissionGraphService
{
    /// <summary>
    ///     Builds a complete permission graph for a tenant showing all relationships.
    /// </summary>
    /// <param name="tenantId">The tenant ID (null for global).</param>
    /// <param name="settings">Optional visualization settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Permission graph with nodes and edges.</returns>
    Task<PermissionGraph> BuildTenantGraphAsync(Guid? tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Builds a permission graph focused on a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID (null for global).</param>
    /// <param name="settings">Optional visualization settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Permission graph centered on the user.</returns>
    Task<PermissionGraph> BuildUserGraphAsync(Guid userId, Guid? tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Builds a permission graph for a specific resource.
    /// </summary>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="tenantId">The tenant ID (null for global).</param>
    /// <param name="settings">Optional visualization settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Permission graph centered on the resource.</returns>
    Task<PermissionGraph> BuildResourceGraphAsync(string resourceId, string resourceType, Guid? tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Analyzes the impact of granting permissions.
    /// </summary>
    /// <param name="userId">The user who would receive permissions.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="permissions">The permissions to grant.</param>
    /// <param name="scopeType">Optional scope type (e.g., "Resource").</param>
    /// <param name="scopeId">Optional scope ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Impact analysis showing affected entities.</returns>
    Task<PermissionImpactAnalysis> AnalyzeGrantImpactAsync(Guid userId, Guid? tenantId, string[ ] permissions, string? scopeType = null, string? scopeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Analyzes the impact of revoking permissions.
    /// </summary>
    /// <param name="userId">The user who would lose permissions.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="permissions">The permissions to revoke.</param>
    /// <param name="scopeType">Optional scope type.</param>
    /// <param name="scopeId">Optional scope ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Impact analysis showing affected entities.</returns>
    Task<PermissionImpactAnalysis> AnalyzeRevokeImpactAsync(Guid userId, Guid? tenantId, string[ ] permissions, string? scopeType = null, string? scopeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Analyzes the impact of modifying a delegation.
    /// </summary>
    /// <param name="delegationId">The delegation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Impact analysis for the delegation.</returns>
    Task<PermissionImpactAnalysis> AnalyzeDelegationImpactAsync(Guid delegationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds all paths between two nodes in the permission graph.
    /// </summary>
    /// <param name="sourceId">The source node ID.</param>
    /// <param name="targetId">The target node ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="maxDepth">Maximum depth to search (default: 5).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of paths, where each path is a list of node IDs.</returns>
    Task<List<List<Guid>>> FindPathsAsync(Guid sourceId, Guid targetId, Guid? tenantId, int maxDepth = 5, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds circular dependencies in the permission graph.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of circular dependency chains.</returns>
    Task<List<List<Guid>>> FindCircularDependenciesAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the most influential nodes (users/resources with most connections).
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="topN">Number of top nodes to return (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of influential nodes with metrics.</returns>
    Task<List<InfluentialNode>> GetInfluentialNodesAsync(Guid? tenantId, int topN = 10, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Exports the permission graph to various formats.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="format">The export format (DOT, JSON, GraphML).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Serialized graph in the requested format.</returns>
    Task<string> ExportGraphAsync(Guid? tenantId, GraphExportFormat format, CancellationToken cancellationToken = default);
}
