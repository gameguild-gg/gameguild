using GameGuild.Modules.FileUpload.Entities;

namespace GameGuild.Modules.FileUpload.Repositories;

public interface IUploadedFileRepository
{
    Task<UploadedFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadedFile>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadedFile>> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadedFile>> GetByStatusAsync(Guid tenantId, FileUploadStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadedFile>> GetPendingScansAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadedFile>> GetQuarantinedFilesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<UploadedFile>> AddAsync(UploadedFile file, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(UploadedFile file, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IScanResultRepository
{
    Task<ScanResult?> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScanResult>> GetByStatusAsync(ScanStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScanResult>> GetThreatsAsync(DateTime? since = null, CancellationToken cancellationToken = default);
    Task<Result<ScanResult>> AddAsync(ScanResult scanResult, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(ScanResult scanResult, CancellationToken cancellationToken = default);
}

public interface IFileMetadataRepository
{
    Task<FileMetadata?> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Result<FileMetadata>> AddAsync(FileMetadata metadata, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(FileMetadata metadata, CancellationToken cancellationToken = default);
}

public interface IUploadChunkRepository
{
    Task<UploadChunk?> GetAsync(Guid fileId, int chunkNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadChunk>> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadChunk>> GetPendingAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UploadChunk>> GetFailedAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Result<UploadChunk>> AddAsync(UploadChunk chunk, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(UploadChunk chunk, CancellationToken cancellationToken = default);
    Task<Result> DeleteAllForFileAsync(Guid fileId, CancellationToken cancellationToken = default);
}
