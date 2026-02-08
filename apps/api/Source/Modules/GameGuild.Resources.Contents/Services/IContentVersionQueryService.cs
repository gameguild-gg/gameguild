namespace GameGuild.Resources.Contents;

/// <summary>
/// Service for querying version history and comparing versions.
/// </summary>
public interface IContentVersionQueryService
{
    /// <summary>Get all versions for an entity</summary>
    Task<Result<IEnumerable<ContentVersion>>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        CancellationToken ct = default);

    /// <summary>Get a specific version</summary>
    Task<Result<ContentVersion>> GetVersionAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>Get a specific version by number</summary>
    Task<Result<ContentVersion>> GetVersionByNumberAsync(
        Guid entityId,
        string entityType,
        int versionNumber,
        CancellationToken ct = default);

    /// <summary>Get the current published version</summary>
    Task<Result<ContentVersion>> GetCurrentVersionAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Compare two versions</summary>
    Task<Result<ContentVersionDiff>> CompareVersionsAsync(
        Guid versionId1,
        Guid versionId2,
        CancellationToken ct = default);
}
