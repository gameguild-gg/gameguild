namespace GameGuild.Modules.FileUpload.Entities;

public class FileMetadata : EntityBase
{
    public Guid FileId { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }
    public int? Bitrate { get; set; }
    public string? Format { get; set; }
    public string? Codec { get; set; }
    public double? FrameRate { get; set; }
    public int? PageCount { get; set; }
    public string? Author { get; set; }
    public string? Title { get; set; }
    public DateTime? CreationDate { get; set; }
    public Dictionary<string, object>? ExifData { get; set; }
    public Dictionary<string, object>? CustomProperties { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? PreviewPath { get; set; }

    // Navigation properties
    public UploadedFile File { get; set; } = null!;

    // Computed properties
    public bool HasDimensions => Width.HasValue && Height.HasValue;
    public bool HasDuration => Duration.HasValue;
    public bool HasThumbnail => !string.IsNullOrEmpty(ThumbnailPath);
    public string AspectRatio => HasDimensions ? $"{Width}:{Height}" : "Unknown";

    // Business methods
    public void SetImageMetadata(int width, int height, string format, Dictionary<string, object>? exifData = null)
    {
        Width = width;
        Height = height;
        Format = format;
        ExifData = exifData;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVideoMetadata(int width, int height, TimeSpan duration, string codec, int bitrate, double frameRate)
    {
        Width = width;
        Height = height;
        Duration = duration;
        Codec = codec;
        Bitrate = bitrate;
        FrameRate = frameRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAudioMetadata(TimeSpan duration, string codec, int bitrate)
    {
        Duration = duration;
        Codec = codec;
        Bitrate = bitrate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDocumentMetadata(int pageCount, string? author = null, string? title = null, DateTime? creationDate = null)
    {
        PageCount = pageCount;
        Author = author;
        Title = title;
        CreationDate = creationDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetThumbnail(string thumbnailPath)
    {
        ThumbnailPath = thumbnailPath;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPreview(string previewPath)
    {
        PreviewPath = previewPath;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCustomProperty(string key, object value)
    {
        if (CustomProperties == null)
            CustomProperties = new Dictionary<string, object>();

        CustomProperties[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
