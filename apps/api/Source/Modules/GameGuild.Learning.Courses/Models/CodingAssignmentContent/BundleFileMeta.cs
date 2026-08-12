namespace GameGuild.Learning.Courses;

/// <summary>
/// Metadata for one bundled file in a coding assignment workspace.
/// </summary>
public sealed record BundleFileMeta
{
    /// <summary>Raw file content (text or base64-encoded bytes).</summary>
    public required string Content { get; init; }

    /// <summary>"text" or "base64".</summary>
    public string Encoding { get; init; } = "text";

    /// <summary>"Public" (visible to learner) or "Private" (hidden). "Solution" is rejected server-side.</summary>
    public string Visibility { get; init; } = "Public";

    /// <summary>Whether the learner may edit this file. Private + Modifiable is rejected.</summary>
    public bool Modifiable { get; init; } = true;
}
