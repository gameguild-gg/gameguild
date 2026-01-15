namespace GameGuild.Assets.Storage;

/// <summary>
/// Supported cloud storage provider types.
/// </summary>
public enum StorageProviderType
{
    /// <summary>
    /// Amazon S3 or S3-compatible services (MinIO, DigitalOcean Spaces, Wasabi, etc.)
    /// </summary>
    S3Compatible = 0,

    /// <summary>
    /// Google Cloud Storage
    /// </summary>
    GoogleCloudStorage = 1,

    /// <summary>
    /// Azure Blob Storage
    /// </summary>
    AzureBlobStorage = 2,

    /// <summary>
    /// Cloudflare R2 (S3-compatible but with different auth)
    /// </summary>
    CloudflareR2 = 3,

    /// <summary>
    /// Backblaze B2 (S3-compatible API available)
    /// </summary>
    BackblazeB2 = 4,

    /// <summary>
    /// Local filesystem (development only)
    /// </summary>
    LocalFileSystem = 99
}
