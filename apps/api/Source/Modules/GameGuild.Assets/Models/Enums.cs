namespace GameGuild.Assets;

/// <summary>
/// Asset content classification.
/// </summary>
public enum AssetKind
{
    /// <summary>Image file (JPEG, PNG, WebP, GIF, etc.)</summary>
    Image = 1,
    
    /// <summary>Video file (MP4, WebM, MOV, etc.)</summary>
    Video = 2,
    
    /// <summary>Audio file (MP3, WAV, FLAC, etc.)</summary>
    Audio = 3,
    
    /// <summary>Document file (PDF, DOCX, etc.)</summary>
    Document = 4,
    
    /// <summary>Archive file (ZIP, RAR, 7z, etc.)</summary>
    Archive = 5,
    
    /// <summary>Other/unknown file type</summary>
    Other = 99
}

/// <summary>
/// Access policy for asset references.
/// </summary>
public enum AssetAccessPolicy
{
    /// <summary>Only owner and admins can access</summary>
    Private = 0,
    
    /// <summary>Accessible via short-lived signed URLs (default for ephemeral)</summary>
    SignedUrl = 1,
    
    /// <summary>Accessible to all authenticated users in tenant</summary>
    TenantPublic = 2,
    
    /// <summary>Publicly accessible (use with caution)</summary>
    Public = 3,
    
    /// <summary>Requires purchase/entitlement to access</summary>
    PaidContent = 4
}

/// <summary>
/// Virus scan status.
/// </summary>
public enum VirusScanStatus
{
    /// <summary>Scan not yet started</summary>
    Pending = 0,
    
    /// <summary>Scan in progress</summary>
    Scanning = 1,
    
    /// <summary>Content is clean</summary>
    Clean = 2,
    
    /// <summary>Virus/malware detected</summary>
    Infected = 3,
    
    /// <summary>Scan failed (retry needed)</summary>
    ScanFailed = 4
}

/// <summary>
/// Content moderation status.
/// </summary>
public enum ModerationStatus
{
    /// <summary>Moderation not yet started</summary>
    Pending = 0,
    
    /// <summary>Auto-moderation in progress</summary>
    Processing = 1,
    
    /// <summary>Content approved for display</summary>
    Approved = 2,
    
    /// <summary>Content rejected, cannot be displayed</summary>
    Rejected = 3,
    
    /// <summary>Needs human review</summary>
    NeedsReview = 4,
    
    /// <summary>Approved but with a content warning</summary>
    ApprovedWithWarning = 5
}

/// <summary>
/// Report reason categories.
/// </summary>
public enum ReportReason
{
    /// <summary>Inappropriate content</summary>
    Inappropriate = 1,
    
    /// <summary>Copyright violation</summary>
    Copyright = 2,
    
    /// <summary>Spam content</summary>
    Spam = 3,
    
    /// <summary>Violence/gore</summary>
    Violence = 4,
    
    /// <summary>Harassment/bullying</summary>
    Harassment = 5,
    
    /// <summary>Misinformation</summary>
    Misinformation = 6,
    
    /// <summary>Other reason</summary>
    Other = 99
}

/// <summary>
/// Report review status.
/// </summary>
public enum ReportStatus
{
    /// <summary>Report submitted, not yet reviewed</summary>
    Pending = 0,
    
    /// <summary>Report is being reviewed</summary>
    UnderReview = 1,
    
    /// <summary>Report has been resolved</summary>
    Resolved = 2,
    
    /// <summary>Report was dismissed</summary>
    Dismissed = 3
}

/// <summary>
/// Moderator review decision.
/// </summary>
public enum ReviewDecision
{
    /// <summary>No action taken</summary>
    NoAction = 0,
    
    /// <summary>Content was removed</summary>
    ContentRemoved = 1,
    
    /// <summary>Content was hidden</summary>
    ContentHidden = 2,
    
    /// <summary>User was warned</summary>
    UserWarned = 3,
    
    /// <summary>User was suspended</summary>
    UserSuspended = 4
}

/// <summary>
/// Image fit mode for transformations.
/// </summary>
public enum ImageFit
{
    /// <summary>Scale down to fit within dimensions, maintaining aspect ratio</summary>
    Contain,
    
    /// <summary>Scale and crop to cover dimensions, maintaining aspect ratio</summary>
    Cover,
    
    /// <summary>Stretch to fill dimensions (may distort)</summary>
    Fill,
    
    /// <summary>Scale down to fit inside dimensions (never enlarge)</summary>
    Inside,
    
    /// <summary>Scale down to cover dimensions (never enlarge)</summary>
    Outside
}

/// <summary>
/// Output image format for transformations.
/// </summary>
public enum ImageFormat
{
    /// <summary>JPEG format (lossy compression)</summary>
    Jpeg,
    
    /// <summary>PNG format (lossless compression)</summary>
    Png,
    
    /// <summary>WebP format (modern, efficient)</summary>
    Webp,
    
    /// <summary>AVIF format (next-gen, highly efficient)</summary>
    Avif,
    
    /// <summary>GIF format (for animations)</summary>
    Gif
}
