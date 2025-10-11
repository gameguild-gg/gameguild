namespace GameGuild.Modules.Authentication;

/// <summary>
/// Entity for tracking identity verification (KYC) status and workflows
/// </summary>
public class IdentityVerification : EntityBase
{
    /// <summary>
    /// User ID associated with this verification
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// KYC verification provider (e.g., "stripe", "persona", "onfido", "jumio")
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// External provider's verification ID
    /// </summary>
    public string ProviderVerificationId { get; private set; } = string.Empty;

    /// <summary>
    /// Verification level (e.g., "basic", "intermediate", "advanced")
    /// </summary>
    public VerificationLevel Level { get; private set; }

    /// <summary>
    /// Current verification status
    /// </summary>
    public VerificationStatus Status { get; private set; }

    /// <summary>
    /// Type of verification performed
    /// </summary>
    public VerificationType Type { get; private set; }

    /// <summary>
    /// Documents submitted for verification (encrypted JSON)
    /// </summary>
    public string? DocumentsSubmitted { get; private set; }

    /// <summary>
    /// Verification result details (encrypted JSON)
    /// </summary>
    public string? ResultDetails { get; private set; }

    /// <summary>
    /// Risk score assigned by provider (0-100)
    /// </summary>
    public int? RiskScore { get; private set; }

    /// <summary>
    /// Compliance flags (AML, sanctions screening, PEP check)
    /// </summary>
    public ComplianceFlags ComplianceFlags { get; private set; }

    /// <summary>
    /// When verification was initiated
    /// </summary>
    public DateTime InitiatedAt { get; private set; }

    /// <summary>
    /// When verification was completed (null if pending)
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// When verification expires (if applicable)
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Reason for rejection (if status is Rejected)
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Reviewer notes (for manual reviews)
    /// </summary>
    public string? ReviewerNotes { get; private set; }

    /// <summary>
    /// IP address when verification was initiated
    /// </summary>
    public string? InitiatedFromIp { get; private set; }

    private IdentityVerification() { }

    /// <summary>
    /// Creates a new identity verification record
    /// </summary>
    public static Result<IdentityVerification> Create(
        Guid userId,
        string provider,
        string providerVerificationId,
        VerificationLevel level,
        VerificationType type,
        string? initiatedFromIp = null)
    {
        if (userId == Guid.Empty)
            return Result<IdentityVerification>.Failure(Error.Validation(
                "IdentityVerification.UserId.Empty",
                "User ID cannot be empty"));

        if (string.IsNullOrWhiteSpace(provider))
            return Result<IdentityVerification>.Failure(Error.Validation(
                "IdentityVerification.Provider.Empty",
                "Provider cannot be empty"));

        if (string.IsNullOrWhiteSpace(providerVerificationId))
            return Result<IdentityVerification>.Failure(Error.Validation(
                "IdentityVerification.ProviderVerificationId.Empty",
                "Provider verification ID cannot be empty"));

        var verification = new IdentityVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderVerificationId = providerVerificationId,
            Level = level,
            Type = type,
            Status = VerificationStatus.Pending,
            ComplianceFlags = ComplianceFlags.None,
            InitiatedAt = DateTime.UtcNow,
            InitiatedFromIp = initiatedFromIp,
            CreatedAt = DateTime.UtcNow
        };

        return Result<IdentityVerification>.Success(verification);
    }

    /// <summary>
    /// Updates verification status to approved
    /// </summary>
    public Result Approve(string? resultDetails = null, int? riskScore = null, TimeSpan? validityPeriod = null)
    {
        if (Status == VerificationStatus.Approved)
            return Result.Failure(Error.Validation(
                "IdentityVerification.AlreadyApproved",
                "Verification is already approved"));

        Status = VerificationStatus.Approved;
        CompletedAt = DateTime.UtcNow;
        ResultDetails = resultDetails;
        RiskScore = riskScore;

        if (validityPeriod.HasValue)
            ExpiresAt = DateTime.UtcNow.Add(validityPeriod.Value);

        Touch();
        return Result.Success();
    }

    /// <summary>
    /// Updates verification status to rejected
    /// </summary>
    public Result Reject(string rejectionReason, string? resultDetails = null, int? riskScore = null)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return Result.Failure(Error.Validation(
                "IdentityVerification.RejectionReason.Empty",
                "Rejection reason is required"));

        Status = VerificationStatus.Rejected;
        CompletedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason;
        ResultDetails = resultDetails;
        RiskScore = riskScore;

        Touch();
        return Result.Success();
    }

    /// <summary>
    /// Updates compliance flags
    /// </summary>
    public void UpdateComplianceFlags(ComplianceFlags flags)
    {
        ComplianceFlags = flags;
        Touch();
    }

    /// <summary>
    /// Adds reviewer notes
    /// </summary>
    public void AddReviewerNotes(string notes)
    {
        ReviewerNotes = notes;
        Touch();
    }

    /// <summary>
    /// Checks if verification is expired
    /// </summary>
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Checks if verification is valid (approved and not expired)
    /// </summary>
    public bool IsValid() => Status == VerificationStatus.Approved && !IsExpired();
}

/// <summary>
/// KYC verification levels
/// </summary>
public enum VerificationLevel
{
    Basic = 1,
    Intermediate = 2,
    Advanced = 3
}

/// <summary>
/// KYC verification status
/// </summary>
public enum VerificationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5
}

/// <summary>
/// Types of identity verification
/// </summary>
public enum VerificationType
{
    DocumentVerification = 1,
    BiometricVerification = 2,
    AddressVerification = 3,
    BankAccountVerification = 4,
    VideoVerification = 5
}

/// <summary>
/// Compliance check flags
/// </summary>
[Flags]
public enum ComplianceFlags
{
    None = 0,
    AmlCleared = 1,
    SanctionsCleared = 2,
    PepCheckCleared = 4,
    WatchlistCleared = 8,
    All = AmlCleared | SanctionsCleared | PepCheckCleared | WatchlistCleared
}
