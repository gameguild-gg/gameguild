namespace GameGuild;

/// <summary> Configuration options for file storage. </summary>
public class FileStorageOptions {
  /// <summary> The storage provider type. </summary>
  public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;

  /// <summary> Base path for local file storage. </summary>
  public string BasePath { get; set; } = "Storage";

  /// <summary> Maximum file size in bytes. </summary>
  public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

  /// <summary> Allowed file extensions. </summary>
  public string[ ] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt"];

  /// <summary> Connection string for cloud storage providers. </summary>
  public string? ConnectionString { get; set; }

  /// <summary> Container/bucket name for cloud storage. </summary>
  public string? ContainerName { get; set; }

  /// <summary> Validates the file storage options. </summary>
  public void Validate() {
    if (string.IsNullOrWhiteSpace(BasePath)) throw new InvalidOperationException("Base path cannot be null or empty.");

    if (MaxFileSizeBytes <= 0) throw new InvalidOperationException("Max file size must be greater than zero.");

    if (Provider == FileStorageProvider.Local) return;

    if (string.IsNullOrWhiteSpace(ConnectionString)) throw new InvalidOperationException("Connection string is required for cloud storage providers.");

    if (string.IsNullOrWhiteSpace(ContainerName)) throw new InvalidOperationException("Container name is required for cloud storage providers.");
  }
}
