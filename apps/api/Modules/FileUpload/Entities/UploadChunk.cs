namespace GameGuild.Modules.FileUpload.Entities;

public class UploadChunk : EntityBase
{
    public Guid FileId { get; set; }
    public int ChunkNumber { get; set; }
    public long SizeInBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public ChunkStatus Status { get; set; } = ChunkStatus.Pending;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public UploadedFile File { get; set; } = null!;

    // Business methods
    public void MarkAsUploaded(string storagePath, string checksum)
    {
        StoragePath = storagePath;
        Checksum = checksum;
        Status = ChunkStatus.Uploaded;
        UploadedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ChunkStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsMerged()
    {
        if (Status != ChunkStatus.Uploaded)
            throw new InvalidOperationException("Can only merge uploaded chunks");

        Status = ChunkStatus.Merged;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanRetry(int maxRetries = 3)
    {
        return Status == ChunkStatus.Failed && RetryCount < maxRetries;
    }

    public void ResetForRetry()
    {
        if (!CanRetry())
            throw new InvalidOperationException("Cannot retry this chunk");

        Status = ChunkStatus.Pending;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ChunkStatus
{
    Pending,
    Uploading,
    Uploaded,
    Merged,
    Failed
}
