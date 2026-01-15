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
        string objectKey,
        string mimeType,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads content from storage.
    /// </summary>
    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Generates a presigned URL for direct download.
    /// </summary>
    Task<string> GeneratePresignedUrlAsync(
        string objectKey,
        TimeSpan expiry,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes content from storage.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Checks if content exists.
    /// </summary>
    Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Gets content metadata.
    /// </summary>
    Task<StorageMetadata?> GetMetadataAsync(string objectKey, CancellationToken ct = default);
}

/// <summary>
/// Result of a storage upload.
/// </summary>
public record StorageUploadResult(
    string BucketName,
    string ObjectKey,
    string ETag,
    long SizeBytes);

/// <summary>
/// Storage content metadata.
/// </summary>
public record StorageMetadata(
    string ObjectKey,
    long SizeBytes,
    string MimeType,
    string ETag,
    DateTime LastModified);
