namespace GameGuild.Learning.Courses;

/// <summary>
/// Workspace file map for a coding assignment: path → metadata.
/// </summary>
public sealed record WorkspaceData
{
    public Dictionary<string, BundleFileMeta> Files { get; init; } = new();
}
