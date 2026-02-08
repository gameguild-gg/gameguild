namespace GameGuild.Resources.Contents;

/// <summary>
/// Service for draft lifecycle management: creation, updates, rollback, and archival.
/// </summary>
public interface IContentDraftService
{
    /// <summary>Create a new draft version for an entity</summary>
    Task<Result<ContentVersion>> CreateDraftAsync(
        Guid entityId,
        string entityType,
        string title,
        Guid createdBy,
        string? summary = null,
        string? body = null,
        string? metadata = null,
        string? changeNotes = null,
        CancellationToken ct = default);

    /// <summary>Update an existing draft version</summary>
    Task<Result<ContentVersion>> UpdateDraftAsync(
        Guid versionId,
        string? title = null,
        string? summary = null,
        string? body = null,
        string? metadata = null,
        string? changeNotes = null,
        CancellationToken ct = default);

    /// <summary>Get the current draft for an entity (if exists)</summary>
    Task<Result<ContentVersion>> GetDraftAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Rollback to a previous version (creates new version based on old)</summary>
    Task<Result<ContentVersion>> RollbackAsync(
        Guid entityId,
        string entityType,
        int targetVersionNumber,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>Archive old versions, keeping only the most recent N versions</summary>
    Task<Result<int>> ArchiveOldVersionsAsync(
        Guid entityId,
        string entityType,
        int keepCount = 10,
        CancellationToken ct = default);
}
