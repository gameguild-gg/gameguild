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
        AssetUploadOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Initializes a chunked upload for large files.
    /// </summary>
    Task<ChunkedUploadSession> InitializeChunkedUploadAsync(
        string fileName,
        string mimeType,
        long totalSize,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a chunk.
    /// </summary>
    Task UploadChunkAsync(
        Guid uploadId,
        int partNumber,
        Stream content,
        CancellationToken ct = default);

    /// <summary>
    /// Completes a chunked upload.
    /// </summary>
    Task<AssetUploadResult> CompleteChunkedUploadAsync(
        Guid uploadId,
        AssetUploadOptions? options = null,
        CancellationToken ct = default);
}

/// <summary>
/// Options for asset upload.
/// </summary>
public record AssetUploadOptions
{
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? AltText { get; init; }
    public AssetAccessPolicy AccessPolicy { get; init; } = AssetAccessPolicy.Private;
    public string? ParentResourceType { get; init; }
    public Guid? ParentResourceId { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>
/// Result of an asset upload.
/// </summary>
public record AssetUploadResult(
    Guid AssetReferenceId,
    Guid AssetContentId,
    string ContentHash,
    long SizeBytes,
    AssetKind Kind,
    bool WasDeduplicated);

/// <summary>
/// Session for chunked upload.
/// </summary>
public record ChunkedUploadSession(
    Guid UploadId,
    string UploadKey,
    int TotalParts,
    DateTime ExpiresAt);
