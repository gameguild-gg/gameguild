using System.Diagnostics;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Constants;
using GameGuild.Modules.Permissions.Entities;


namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for graph-based permission visualization and impact analysis
/// </summary>
public class PermissionGraphService : IPermissionGraphService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IDelegatedAdminService _delegatedAdminService;
    private readonly ILogger<PermissionGraphService> _logger;

    public PermissionGraphService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        IDelegatedAdminService delegatedAdminService,
        ILogger<PermissionGraphService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _delegatedAdminService = delegatedAdminService ?? throw new ArgumentNullException(nameof(delegatedAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PermissionGraph> BuildTenantGraphAsync(Guid tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default)
    {
        settings ??= new PermissionVisualizationSettings();
        var graph = new PermissionGraph();

        _logger.LogInformation("Building permission graph for tenant {TenantId}", tenantId);

        // Add tenant node
        graph.AddNode(new PermissionGraphNode
        {
            Id = tenantId,
            NodeType = "Tenant",
            Name = $"Tenant {tenantId}",
            Attributes = new Dictionary<string, object> { { "Type", "Tenant" } }
        });

        // Load and add user nodes with their permissions
        var tenantPermissions = await _context.Set<TenantPermission>()
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        foreach (var permission in tenantPermissions)
        {
            if (!permission.UserId.HasValue) continue;

            var userNode = new PermissionGraphNode
            {
                Id = permission.UserId.Value,
                NodeType = "User",
                Name = $"User {permission.UserId}",
                Attributes = new Dictionary<string, object>
                {
                    { "PermissionCount", permission.PermissionFlags1.ToString() }
                }
            };
            graph.AddNode(userNode);

            graph.AddEdge(new PermissionGraphEdge
            {
                SourceId = tenantId,
                TargetId = permission.UserId.Value,
                RelationType = "HasMember",
                Properties = new Dictionary<string, object> { { "PermissionFlags", permission.PermissionFlags1 } }
            });
        }

        // Add delegations
        var delegations = await _context.Set<DelegatedAdminScope>()
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var delegation in delegations)
        {
            graph.AddEdge(new PermissionGraphEdge
            {
                SourceId = delegation.DelegatorUserId,
                TargetId = delegation.DelegatedUserId,
                RelationType = "Delegates",
                Properties = new Dictionary<string, object>
                {
                    { "ScopeType", delegation.ScopeType },
                    { "Permissions", delegation.DelegatedPermissions.Select(p => p.ToString()).ToArray() }
                }
            });
        }

        graph.Metadata = new Dictionary<string, object>
        {
            { "TenantId", tenantId },
            { "NodeCount", graph.Nodes.Count },
            { "EdgeCount", graph.Edges.Count },
            { "GeneratedAt", DateTime.UtcNow }
        };

        return graph;
    }

    public async Task<PermissionGraph> BuildUserGraphAsync(Guid userId, Guid tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default)
    {
        settings ??= new PermissionVisualizationSettings();
        var graph = new PermissionGraph();

        _logger.LogInformation("Building permission graph for user {UserId} in tenant {TenantId}", userId, tenantId);

        // Add user node
        graph.AddNode(new PermissionGraphNode
        {
            Id = userId,
            NodeType = "User",
            Name = $"User {userId}",
            Attributes = new Dictionary<string, object> { { "TenantId", tenantId } }
        });

        // Add delegations from this user
        var delegationsFrom = await _delegatedAdminService.GetDelegationsByDelegatorAsync(userId, tenantId, cancellationToken: cancellationToken);
        foreach (var delegation in delegationsFrom)
        {
            graph.AddNode(new PermissionGraphNode
            {
                Id = delegation.DelegatedUserId,
                NodeType = "User",
                Name = $"User {delegation.DelegatedUserId}"
            });

            graph.AddEdge(new PermissionGraphEdge
            {
                SourceId = userId,
                TargetId = delegation.DelegatedUserId,
                RelationType = "Delegates",
                Properties = new Dictionary<string, object>
                {
                    { "Permissions", delegation.DelegatedPermissions.Select(p => p.ToString()).ToArray() }
                }
            });
        }

        // Add delegations to this user
        var delegationsTo = await _delegatedAdminService.GetUserDelegationsAsync(userId, tenantId, cancellationToken: cancellationToken);
        foreach (var delegation in delegationsTo)
        {
            graph.AddNode(new PermissionGraphNode
            {
                Id = delegation.DelegatorUserId,
                NodeType = "User",
                Name = $"User {delegation.DelegatorUserId}"
            });

            graph.AddEdge(new PermissionGraphEdge
            {
                SourceId = delegation.DelegatorUserId,
                TargetId = userId,
                RelationType = "Delegates",
                Properties = new Dictionary<string, object>
                {
                    { "Permissions", delegation.DelegatedPermissions.Select(p => p.ToString()).ToArray() }
                }
            });
        }

        return graph;
    }

    public async Task<PermissionGraph> BuildResourceGraphAsync(Guid resourceId, string resourceType, Guid tenantId, PermissionVisualizationSettings? settings = null, CancellationToken cancellationToken = default)
    {
        var graph = new PermissionGraph();

        graph.AddNode(new PermissionGraphNode
        {
            Id = resourceId,
            NodeType = "Resource",
            Name = $"{resourceType} {resourceId}",
            Attributes = new Dictionary<string, object>
            {
                { "ResourceType", resourceType },
                { "TenantId", tenantId }
            }
        });

        // Would load resource permissions here

        return graph;
    }

    public async Task<PermissionImpactAnalysis> AnalyzeGrantImpactAsync(Guid userId, Guid tenantId, PermissionType[] permissions, string? scopeType = null, Guid? scopeId = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var analysis = new PermissionImpactAnalysis
        {
            TargetEntityId = userId,
            TargetEntityType = "User",
            AffectedPermissions = permissions
        };

        _logger.LogInformation("Analyzing grant impact for user {UserId}, permissions: {Permissions}", userId, string.Join(", ", permissions));

        // Check if user already has these permissions
        var existingPerms = await _permissionService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        var newPermissions = permissions.Except(existingPerms).ToArray();

        if (newPermissions.Length == 0)
        {
            analysis.Severity = ImpactSeverity.Low;
            analysis.Warnings.Add("User already has all specified permissions");
        }
        else
        {
            analysis.DirectlyImpacted.Add(new ImpactedEntity
            {
                EntityId = userId,
                EntityType = "User",
                EntityName = $"User {userId}",
                ImpactedPermissions = newPermissions.Select(p => p.ToString()).ToArray(),
                ImpactType = "Granted",
                HopsFromSource = 0
            });

            analysis.TotalUsersAffected = 1;
            analysis.Severity = newPermissions.Any(p => TenantPermissionConstants.AdminPermissions.Contains(p))
                ? ImpactSeverity.High
                : ImpactSeverity.Medium;
        }

        // Check for delegation impacts
        var delegations = await _delegatedAdminService.GetDelegationsByDelegatorAsync(userId, tenantId, cancellationToken: cancellationToken);
        foreach (var delegation in delegations)
        {
            analysis.IndirectlyImpacted.Add(new ImpactedEntity
            {
                EntityId = delegation.DelegatedUserId,
                EntityType = "User",
                EntityName = $"User {delegation.DelegatedUserId}",
                ImpactedPermissions = permissions.Intersect(delegation.DelegatedPermissions).Select(p => p.ToString()).ToArray(),
                ImpactType = "Inherited",
                HopsFromSource = 1
            });
        }

        analysis.TotalDelegationsAffected = delegations.Count();
        analysis.AnalysisDuration = stopwatch.Elapsed;

        return analysis;
    }

    public async Task<PermissionImpactAnalysis> AnalyzeRevokeImpactAsync(Guid userId, Guid tenantId, PermissionType[] permissions, string? scopeType = null, Guid? scopeId = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var analysis = new PermissionImpactAnalysis
        {
            TargetEntityId = userId,
            TargetEntityType = "User",
            AffectedPermissions = permissions,
            Severity = ImpactSeverity.Medium
        };

        _logger.LogInformation("Analyzing revoke impact for user {UserId}, permissions: {Permissions}", userId, string.Join(", ", permissions));

        analysis.DirectlyImpacted.Add(new ImpactedEntity
        {
            EntityId = userId,
            EntityType = "User",
            EntityName = $"User {userId}",
            ImpactedPermissions = permissions.Select(p => p.ToString()).ToArray(),
            ImpactType = "Revoked",
            HopsFromSource = 0
        });

        // Check delegation impact
        var delegations = await _delegatedAdminService.GetDelegationsByDelegatorAsync(userId, tenantId, cancellationToken: cancellationToken);
        foreach (var delegation in delegations.Where(d => d.DelegatedPermissions.Intersect(permissions).Any()))
        {
            analysis.IndirectlyImpacted.Add(new ImpactedEntity
            {
                EntityId = delegation.DelegatedUserId,
                EntityType = "User",
                EntityName = $"User {delegation.DelegatedUserId}",
                ImpactedPermissions = permissions.Intersect(delegation.DelegatedPermissions).Select(p => p.ToString()).ToArray(),
                ImpactType = "Revoked",
                HopsFromSource = 1
            });

            analysis.Warnings.Add($"Delegation to user {delegation.DelegatedUserId} will be affected");
        }

        analysis.TotalUsersAffected = 1 + analysis.IndirectlyImpacted.Count;
        analysis.TotalDelegationsAffected = delegations.Count(d => d.DelegatedPermissions.Intersect(permissions).Any());

        if (permissions.Any(p => TenantPermissionConstants.AdminPermissions.Contains(p)))
        {
            analysis.Severity = ImpactSeverity.Critical;
            analysis.Warnings.Add("Revoking administrative permissions");
        }

        analysis.AnalysisDuration = stopwatch.Elapsed;
        return analysis;
    }

    public async Task<PermissionImpactAnalysis> AnalyzeDelegationImpactAsync(Guid delegationId, CancellationToken cancellationToken = default)
    {
        var delegation = await _delegatedAdminService.GetDelegationByIdAsync(delegationId, cancellationToken);
        if (delegation == null)
            throw new InvalidOperationException("Delegation not found");

        return await AnalyzeRevokeImpactAsync(
            delegation.DelegatedUserId,
            delegation.TenantId,
            delegation.DelegatedPermissions,
            delegation.ScopeType,
            delegation.ScopeId,
            cancellationToken);
    }

    public async Task<List<List<PermissionGraphNode>>> FindPathsAsync(Guid sourceId, Guid targetId, Guid tenantId, int maxDepth = 5, CancellationToken cancellationToken = default)
    {
        var graph = await BuildTenantGraphAsync(tenantId, cancellationToken: cancellationToken);
        var paths = new List<List<PermissionGraphNode>>();

        // BFS to find all paths
        var queue = new Queue<(Guid current, List<Guid> path)>();
        queue.Enqueue((sourceId, new List<Guid> { sourceId }));

        while (queue.Any())
        {
            var (current, path) = queue.Dequeue();

            if (path.Count > maxDepth) continue;
            if (current == targetId)
            {
                paths.Add(path.Select(id => graph.Nodes.First(n => n.Id == id)).ToList());
                continue;
            }

            foreach (var neighbor in graph.GetNeighbors(current))
            {
                if (!path.Contains(neighbor.Id))
                {
                    var newPath = new List<Guid>(path) { neighbor.Id };
                    queue.Enqueue((neighbor.Id, newPath));
                }
            }
        }

        return paths;
    }

    public async Task<List<List<Guid>>> FindCircularDependenciesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var graph = await BuildTenantGraphAsync(tenantId, cancellationToken: cancellationToken);
        var cycles = new List<List<Guid>>();
        var visited = new HashSet<Guid>();
        var recursionStack = new HashSet<Guid>();

        void DFS(Guid nodeId, List<Guid> path)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);
            path.Add(nodeId);

            foreach (var neighbor in graph.GetNeighbors(nodeId))
            {
                if (!visited.Contains(neighbor.Id))
                {
                    DFS(neighbor.Id, new List<Guid>(path));
                }
                else if (recursionStack.Contains(neighbor.Id))
                {
                    var cycleStart = path.IndexOf(neighbor.Id);
                    var cycle = path.Skip(cycleStart).ToList();
                    cycles.Add(cycle);
                }
            }

            recursionStack.Remove(nodeId);
        }

        foreach (var node in graph.Nodes)
        {
            if (!visited.Contains(node.Id))
                DFS(node.Id, new List<Guid>());
        }

        return cycles;
    }

    public async Task<List<InfluentialNode>> GetInfluentialNodesAsync(Guid tenantId, int topN = 10, CancellationToken cancellationToken = default)
    {
        var graph = await BuildTenantGraphAsync(tenantId, cancellationToken: cancellationToken);

        return graph.Nodes.Select(node =>
        {
            var incoming = graph.GetIncomingEdges(node.Id).Count();
            var outgoing = graph.GetOutgoingEdges(node.Id).Count();
            return new InfluentialNode
            {
                NodeId = node.Id,
                NodeType = node.NodeType,
                Name = node.Name,
                IncomingEdges = incoming,
                OutgoingEdges = outgoing,
                TotalConnections = incoming + outgoing,
                CentralityScore = (incoming * 1.5) + outgoing // Weight incoming edges more
            };
        })
        .OrderByDescending(n => n.CentralityScore)
        .Take(topN)
        .ToList();
    }

    public async Task<string> ExportGraphAsync(Guid tenantId, GraphExportFormat format, CancellationToken cancellationToken = default)
    {
        var graph = await BuildTenantGraphAsync(tenantId, cancellationToken: cancellationToken);

        return format switch
        {
            GraphExportFormat.JSON => JsonSerializer.Serialize(graph, new JsonSerializerOptions { WriteIndented = true }),
            GraphExportFormat.DOT => ExportToDOT(graph),
            _ => throw new NotImplementedException($"Export format {format} not implemented")
        };
    }

    private string ExportToDOT(PermissionGraph graph)
    {
        var lines = new List<string> { "digraph PermissionGraph {" };

        foreach (var node in graph.Nodes)
            lines.Add($"  \"{node.Id}\" [label=\"{node.Name}\" shape=\"{(node.NodeType == "User" ? "ellipse" : "box")}\"];");

        foreach (var edge in graph.Edges)
            lines.Add($"  \"{edge.SourceId}\" -> \"{edge.TargetId}\" [label=\"{edge.RelationType}\"];");

        lines.Add("}");
        return string.Join(Environment.NewLine, lines);
    }
}
