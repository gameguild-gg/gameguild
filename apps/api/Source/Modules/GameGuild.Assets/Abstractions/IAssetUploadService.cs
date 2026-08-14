namespace GameGuild.Assets;

/// <summary>
/// Service for uploading assets to storage.
/// </summary>
public interface IAssetUploadService
{
    /// <summary>
    /// Uploads a new asset from a stream.
    /// </summary>
    Task<AssetUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        Guid userId,
        UploadAssetOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Initializes a chunked upload for large files.
    /// </summary>
    Task<ChunkedUploadSession> InitiateChunkedUploadAsync(
        string fileName,
        string mimeType,
        long totalSize,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a chunk.
    /// </summary>
    Task<bool> UploadChunkAsync(
        string uploadId,
        int chunkIndex,
        Stream chunkContent,
        CancellationToken ct = default);

    /// <summary>
    /// Completes a chunked upload.
    /// </summary>
    Task<AssetUploadResult> CompleteChunkedUploadAsync(
        string uploadId,
        UploadAssetOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Aborts a chunked upload.
    /// </summary>
    Task AbortChunkedUploadAsync(
        string uploadId,
        CancellationToken ct = default);
}

/// <summary>
/// Options for asset upload.
/// </summary>
public record UploadAssetOptions(
    string? DisplayName = null,
    AssetAccessPolicy AccessPolicy = AssetAccessPolicy.Private,
    string? ParentResourceType = null,
    Guid? ParentResourceId = null,
    Guid? FolderId = null,
    Guid? TenantId = null);

/// <summary>
/// Result of an asset upload.
/// </summary>
public sealed record AssetUploadResult(
    bool Success,
    Guid? AssetReferenceId,
    Guid? AssetContentId,
    string? Error);

/// <summary>
/// Session for chunked upload.
/// </summary>
public record ChunkedUploadSession(
    string UploadId,
    string ObjectKey,
    Guid UserId,
    string FileName,
    string MimeType,
    long TotalSize,
    int TotalChunks,
    DateTime ExpiresAt,
    int UploadedChunks = 0);
