namespace GameGuild.Modules.FileUpload.Services;

public interface IVirusScannerService
{
    Task<ScanResultDto> ScanFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<ScanResultDto> ScanFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
    Task<string> GetSignatureVersionAsync(CancellationToken cancellationToken = default);
}

public interface IFormatValidatorService
{
    Task<ValidationResult> ValidateAsync(Stream fileStream, string fileName, FileCategory expectedCategory, CancellationToken cancellationToken = default);
    bool IsSupportedFormat(string contentType, FileCategory category);
    long GetMaxFileSizeForCategory(FileCategory category);
    string[] GetAllowedExtensions(FileCategory category);
}

public interface IStorageProvider
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<string> UploadChunkAsync(Stream chunkStream, string fileName, int chunkNumber, CancellationToken cancellationToken = default);
    Task<string> MergeChunksAsync(string fileName, int totalChunks, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string filePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string?> GetPublicUrlAsync(string filePath, CancellationToken cancellationToken = default);
    string ProviderName { get; }
}

public record ScanResultDto(
    bool IsClean,
    string? ThreatName,
    string? ThreatDescription,
    string ThreatLevel,
    TimeSpan ScanDuration,
    string SignatureVersion);

public record ValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string? DetectedFormat,
    long FileSize,
    Dictionary<string, object>? Metadata);
