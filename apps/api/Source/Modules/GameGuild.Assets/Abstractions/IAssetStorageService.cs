namespace GameGuild.Assets;

/// <summary>
/// Service for S3-compatible storage operations.
/// </summary>
public interface IAssetStorageService
{
    /// <summary>
    /// Uploads content to storage.
    /// </summary>
    Task<StorageUploadResult> UploadAsync(
        Stream content,
        string contentHash,
        string mimeType,
        bool isTransformed = false,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads content from storage.
    /// </summary>
    Task<Stream> DownloadAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a presigned URL for direct download or upload.
    /// </summary>
    Task<string> GeneratePresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        bool isDownload = true,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes content from storage.
    /// </summary>
    Task DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if content exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets content metadata.
    /// </summary>
    Task<StorageMetadata?> GetMetadataAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default);

    /// <summary>
    /// Initiates a multipart upload for large files.
    /// </summary>
    Task<string> InitiateMultipartUploadAsync(
        string mimeType,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a part in a multipart upload.
    /// </summary>
    Task<string> UploadPartAsync(
        string uploadId,
        string objectKey,
        int partNumber,
        Stream content,
        CancellationToken ct = default);

    /// <summary>
    /// Completes a multipart upload.
    /// </summary>
    Task<StorageUploadResult> CompleteMultipartUploadAsync(
        string uploadId,
        string objectKey,
        IReadOnlyList<string> partETags,
        CancellationToken ct = default);

    /// <summary>
    /// Aborts a multipart upload.
    /// </summary>
    Task AbortMultipartUploadAsync(
        string uploadId,
        string objectKey,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads content to quarantine bucket (for infected files).
    /// </summary>
    Task UploadToQuarantineAsync(
        Stream content,
        string objectKey,
        IDictionary<string, string> metadata,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a storage upload.
/// </summary>
public record StorageUploadResult(
    string BucketName,
    string ObjectKey,
    string? ETag = null,
    long? SizeBytes = null);

/// <summary>
/// Storage content metadata.
/// </summary>
public record StorageMetadata(
    long SizeBytes,
    string MimeType,
    string ETag,
    DateTime LastModified);
