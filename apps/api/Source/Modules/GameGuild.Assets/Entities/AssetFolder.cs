using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace GameGuild.Assets;

public enum AssetFolderRestrictionMode
{
    None = 0,
    SelectedTeams = 1,
    TeamAuthorities = 2,
    AllocatedProjectMembers = 3
}

/// <summary>A virtual folder scoped to exactly one Team or Project library.</summary>
public sealed class AssetFolder : EntityBase
{
    private AssetFolder() { }

    [Required, MaxLength(100)] public string ParentResourceType { get; private set; } = string.Empty;
    public Guid ParentResourceId { get; private set; }
    public Guid? ParentFolderId { get; private set; }
    [Required, MaxLength(255)] public string Name { get; private set; } = string.Empty;
    public AssetFolderRestrictionMode RestrictionMode { get; private set; }
    [MaxLength(4000)] public string? AllowedTeamIdsJson { get; private set; }
    [MaxLength(2000)] public string? AllowedAuthoritiesJson { get; private set; }

    public IReadOnlyList<Guid> AllowedTeamIds => Deserialize<Guid>(AllowedTeamIdsJson);
    public IReadOnlyList<string> AllowedAuthorities => Deserialize<string>(AllowedAuthoritiesJson);

    public static AssetFolder Create(
        Guid tenantId,
        string parentResourceType,
        Guid parentResourceId,
        Guid? parentFolderId,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentResourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new AssetFolder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParentResourceType = parentResourceType.Trim(),
            ParentResourceId = parentResourceId,
            ParentFolderId = parentFolderId,
            Name = name.Trim()
        };
    }

    public bool BelongsTo(string resourceType, Guid resourceId) =>
        ParentResourceId == resourceId &&
        string.Equals(CanonicalResourceType(ParentResourceType), CanonicalResourceType(resourceType), StringComparison.OrdinalIgnoreCase);

    public void SetRestriction(
        AssetFolderRestrictionMode mode,
        IEnumerable<Guid>? allowedTeamIds = null,
        IEnumerable<string>? allowedAuthorities = null)
    {
        RestrictionMode = mode;
        AllowedTeamIdsJson = Serialize(allowedTeamIds?.Distinct().ToArray());
        AllowedAuthoritiesJson = Serialize(allowedAuthorities?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray());
        Touch();
    }

    private static string? Serialize<T>(IReadOnlyCollection<T>? values) =>
        values is { Count: > 0 } ? JsonSerializer.Serialize(values) : null;

    private static IReadOnlyList<T> Deserialize<T>(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : JsonSerializer.Deserialize<T[]>(value) ?? [];

    private static string CanonicalResourceType(string value) => value.Trim() switch
    {
        var candidate when candidate.Equals("projects", StringComparison.OrdinalIgnoreCase) => "Project",
        var candidate when candidate.Equals("teams", StringComparison.OrdinalIgnoreCase) => "Team",
        var candidate => candidate
    };
}
