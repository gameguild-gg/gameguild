using GameGuild.Modules.Permissions.Constants;

namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a node in the permission graph (user, role, resource, permission)
/// </summary>
public class PermissionGraphNode
{
    public Guid Id { get; set; }
    public string NodeType { get; set; } = null!; // "User", "Template", "Resource", "Permission", "Delegation"
    public string Name { get; set; } = null!;
    public Dictionary<string, object>? Attributes { get; set; }
}

/// <summary>
/// Represents an edge/relationship in the permission graph
/// </summary>
public class PermissionGraphEdge
{
    public Guid SourceId { get; set; }
    public Guid TargetId { get; set; }
    public string RelationType { get; set; } = null!; // "HasPermission", "Inherits", "Delegates", "AppliesTo"
    public Dictionary<string, object>? Properties { get; set; }
    public double Weight { get; set; } = 1.0;
}

/// <summary>
/// Complete permission graph with nodes and edges
/// </summary>
public class PermissionGraph
{
    public List<PermissionGraphNode> Nodes { get; set; } = new();
    public List<PermissionGraphEdge> Edges { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }

    public void AddNode(PermissionGraphNode node)
    {
        if (!Nodes.Any(n => n.Id == node.Id))
            Nodes.Add(node);
    }

    public void AddEdge(PermissionGraphEdge edge)
    {
        if (!Edges.Any(e => e.SourceId == edge.SourceId && e.TargetId == edge.TargetId && e.RelationType == edge.RelationType))
            Edges.Add(edge);
    }

    public IEnumerable<PermissionGraphNode> GetNeighbors(Guid nodeId)
    {
        var neighborIds = Edges
            .Where(e => e.SourceId == nodeId)
            .Select(e => e.TargetId)
            .ToHashSet();

        return Nodes.Where(n => neighborIds.Contains(n.Id));
    }

    public IEnumerable<PermissionGraphEdge> GetIncomingEdges(Guid nodeId)
    {
        return Edges.Where(e => e.TargetId == nodeId);
    }

    public IEnumerable<PermissionGraphEdge> GetOutgoingEdges(Guid nodeId)
    {
        return Edges.Where(e => e.SourceId == nodeId);
    }
}

/// <summary>
/// Impact analysis result for a permission change
/// </summary>
public class PermissionImpactAnalysis
{
    public Guid TargetEntityId { get; set; }
    public string TargetEntityType { get; set; } = null!;
    public PermissionType[] AffectedPermissions { get; set; } = Array.Empty<PermissionType>();

    public List<ImpactedEntity> DirectlyImpacted { get; set; } = new();
    public List<ImpactedEntity> IndirectlyImpacted { get; set; } = new();

    public int TotalUsersAffected { get; set; }
    public int TotalResourcesAffected { get; set; }
    public int TotalDelegationsAffected { get; set; }

    public ImpactSeverity Severity { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    public Dictionary<string, int> ImpactByType { get; set; } = new();
    public TimeSpan AnalysisDuration { get; set; }
}

/// <summary>
/// An entity impacted by a permission change
/// </summary>
public class ImpactedEntity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string[] ImpactedPermissions { get; set; } = Array.Empty<string>();
    public string ImpactType { get; set; } = null!; // "Granted", "Revoked", "Modified", "Inherited"
    public int HopsFromSource { get; set; }
    public List<Guid> PathFromSource { get; set; } = new();
}

/// <summary>
/// Severity of permission impact
/// </summary>
public enum ImpactSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Permission visualization settings
/// </summary>
public class PermissionVisualizationSettings
{
    public int MaxDepth { get; set; } = 5;
    public bool IncludeInactiveNodes { get; set; } = false;
    public bool IncludeExpiredPermissions { get; set; } = false;
    public string[]? NodeTypesFilter { get; set; }
    public string[]? RelationTypesFilter { get; set; }
    public string LayoutAlgorithm { get; set; } = "ForceDirected"; // ForceDirected, Hierarchical, Circular
    public bool GroupByTenant { get; set; } = true;
    public bool ShowPermissionDetails { get; set; } = true;
}
