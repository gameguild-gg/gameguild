using GameGuild.Core.Domain;

namespace GameGuild.Modules.FileUpload.Entities;

public class UploadedFile : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? PublicUrl { get; set; }
    public FileUploadStatus Status { get; set; } = FileUploadStatus.Uploading;
    public DateTime? CompletedAt { get; set; }
    public string? Checksum { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public bool IsChunked { get; set; }
    public int? TotalChunks { get; set; }
    public int? UploadedChunks { get; set; }
    public FileCategory Category { get; set; } = FileCategory.Other;

    // Navigation properties
    public ScanResult? ScanResult { get; set; }
    public FileMetadata? FileMetadata { get; set; }
    public ICollection<UploadChunk> Chunks { get; set; } = new List<UploadChunk>();

    // Computed properties
    public bool IsComplete => Status == FileUploadStatus.Completed;
    public bool IsScanComplete => ScanResult != null;
    public bool IsSafe => ScanResult?.IsClean == true;
    public double UploadProgress => IsChunked && TotalChunks.HasValue && TotalChunks.Value > 0
        ? (double)(UploadedChunks ?? 0) / TotalChunks.Value * 100
        : (Status == FileUploadStatus.Completed ? 100 : 0);

    // Business methods
    public void MarkAsCompleted()
    {
        if (Status == FileUploadStatus.Failed)
            throw new InvalidOperationException("Cannot complete a failed upload");

        Status = FileUploadStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status = FileUploadStatus.Failed;
        if (Metadata == null)
            Metadata = new Dictionary<string, object>();
        Metadata["FailureReason"] = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordChunkUpload(int chunkNumber)
    {
        if (!IsChunked)
            throw new InvalidOperationException("This is not a chunked upload");

        UploadedChunks = (UploadedChunks ?? 0) + 1;
        UpdatedAt = DateTime.UtcNow;

        if (UploadedChunks == TotalChunks)
        {
            Status = FileUploadStatus.Processing;
        }
    }

    public void SetPublicUrl(string url)
    {
        PublicUrl = url;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsValidForCategory()
    {
        return Category switch
        {
            FileCategory.Image => ContentType.StartsWith("image/"),
            FileCategory.Video => ContentType.StartsWith("video/"),
            FileCategory.Audio => ContentType.StartsWith("audio/"),
            FileCategory.Document => ContentType.Contains("pdf") || ContentType.Contains("word") || ContentType.Contains("text"),
            FileCategory.Archive => ContentType.Contains("zip") || ContentType.Contains("rar") || ContentType.Contains("tar"),
            FileCategory.Other => true,
            _ => false
        };
    }
}

public enum FileUploadStatus
{
    Uploading,
    Processing,
    Scanning,
    Completed,
    Failed,
    Quarantined
}

public enum FileCategory
{
    Image,
    Video,
    Audio,
    Document,
    Archive,
    Other
}
