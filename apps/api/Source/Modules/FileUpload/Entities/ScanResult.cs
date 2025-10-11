namespace GameGuild.Modules.FileUpload.Entities;

public class ScanResult : EntityBase
{
    public Guid FileId { get; set; }
    public string ScannerName { get; set; } = "ClamAV";
    public string ScannerVersion { get; set; } = string.Empty;
    public bool IsClean { get; set; }
    public DateTime ScannedAt { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Pending;
    public string? ThreatName { get; set; }
    public string? ThreatDescription { get; set; }
    public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.None;
    public Dictionary<string, object>? ScanDetails { get; set; }
    public TimeSpan ScanDuration { get; set; }
    public string? SignatureVersion { get; set; }
    public bool WasQuarantined { get; set; }
    public DateTime? QuarantinedAt { get; set; }

    // Navigation properties
    public UploadedFile File { get; set; } = null!;

    // Business methods
    public void MarkAsClean(TimeSpan scanDuration, string signatureVersion)
    {
        IsClean = true;
        Status = ScanStatus.Completed;
        ThreatLevel = ThreatLevel.None;
        ScannedAt = DateTime.UtcNow;
        ScanDuration = scanDuration;
        SignatureVersion = signatureVersion;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsThreat(string threatName, string threatDescription, ThreatLevel level, TimeSpan scanDuration, string signatureVersion)
    {
        IsClean = false;
        Status = ScanStatus.ThreatDetected;
        ThreatName = threatName;
        ThreatDescription = threatDescription;
        ThreatLevel = level;
        ScannedAt = DateTime.UtcNow;
        ScanDuration = scanDuration;
        SignatureVersion = signatureVersion;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Quarantine()
    {
        if (IsClean)
            throw new InvalidOperationException("Cannot quarantine a clean file");

        WasQuarantined = true;
        QuarantinedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status = ScanStatus.Failed;
        if (ScanDetails == null)
            ScanDetails = new Dictionary<string, object>();
        ScanDetails["FailureReason"] = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public string GetSummary()
    {
        if (Status == ScanStatus.Pending)
            return "Scan pending";
        if (Status == ScanStatus.InProgress)
            return "Scanning in progress...";
        if (Status == ScanStatus.Failed)
            return $"Scan failed: {ScanDetails?["FailureReason"]}";
        if (IsClean)
            return $"Clean (scanned in {ScanDuration.TotalSeconds:F2}s)";

        return $"Threat detected: {ThreatName} ({ThreatLevel})";
    }
}

public enum ScanStatus
{
    Pending,
    InProgress,
    Completed,
    ThreatDetected,
    Failed
}

public enum ThreatLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}
