using GameGuild.CQRS;
using GameGuild.Modules.FileUpload.Entities;

namespace GameGuild.Modules.FileUpload.Services;

public interface IFileUploadService
{
    Task<Result<FileUploadDto>> InitiateUploadAsync(InitiateUploadRequest request, CancellationToken cancellationToken = default);
    Task<Result<FileUploadDto>> UploadFileAsync(Guid tenantId, Guid userId, Stream fileStream, string fileName, string contentType, FileCategory category, CancellationToken cancellationToken = default);
    Task<Result<ChunkUploadDto>> UploadChunkAsync(Guid fileId, int chunkNumber, Stream chunkStream, CancellationToken cancellationToken = default);
    Task<Result<FileUploadDto>> CompleteChunkedUploadAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Result<FileUploadDto>> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Result<List<FileUploadDto>>> GetFilesAsync(Guid tenantId, Guid? userId, FileUploadStatus? status, CancellationToken cancellationToken = default);
    Task<Result<Stream>> DownloadFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Result> DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Result<ScanStatusDto>> GetScanStatusAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Result<FileMetadataDto>> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);
}

public record InitiateUploadRequest(
    Guid TenantId,
    Guid UserId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    FileCategory Category,
    bool IsChunked,
    int? TotalChunks);

public record FileUploadDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    string Status,
    string Category,
    DateTime? CompletedAt,
    string? PublicUrl,
    bool IsChunked,
    int? TotalChunks,
    int? UploadedChunks,
    double UploadProgress,
    ScanStatusDto? ScanStatus,
    FileMetadataDto? Metadata);

public record ChunkUploadDto(
    Guid FileId,
    int ChunkNumber,
    long SizeInBytes,
    string Status,
    DateTime UploadedAt);

public record ScanStatusDto(
    bool IsScanned,
    bool? IsClean,
    string Status,
    string? ThreatName,
    string? ThreatLevel,
    DateTime? ScannedAt,
    string? Summary);

public record FileMetadataDto(
    int? Width,
    int? Height,
    TimeSpan? Duration,
    string? Format,
    string? Codec,
    int? PageCount,
    string? Author,
    string? Title,
    string? ThumbnailUrl,
    string? PreviewUrl,
    Dictionary<string, object>? CustomProperties);
