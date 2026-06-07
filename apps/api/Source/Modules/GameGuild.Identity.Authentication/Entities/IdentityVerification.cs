using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents an identity verification record.
///     Tracks KYC (Know Your Customer) verification status and documentation.
/// </summary>
public class IdentityVerification
{
    /// <summary>
    ///     Unique identifier for the verification record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The user being verified.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Type of verification (e.g., "Email", "Phone", "Government ID", "Biometric").
    /// </summary>
    [MaxLength(128)]
    public string VerificationType { get; set; } = string.Empty;

    /// <summary>
    ///     Current status of the verification.
    /// </summary>
    [MaxLength(64)]
    public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected, Expired

    /// <summary>
    ///     The value being verified (email address, phone number, etc.) - may be masked.
    /// </summary>
    [MaxLength(256)]
    public string VerifiedValue { get; set; } = string.Empty;

    /// <summary>
    ///     When the verification was initiated.
    /// </summary>
    public DateTime InitiatedAt { get; set; }

    /// <summary>
    ///     When the verification was completed or last updated.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    ///     When the verification expires and needs renewal.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     External verification provider (e.g., "Twilio", "Onfido", "Jumio").
    /// </summary>
    [MaxLength(256)]
    public string? VerificationProvider { get; set; }

    /// <summary>
    ///     External provider's verification ID for reference.
    /// </summary>
    [MaxLength(256)]
    public string? ExternalVerificationId { get; set; }

    /// <summary>
    ///     Verification confidence score (0-1) from the provider.
    /// </summary>
    public double? ConfidenceScore { get; set; }

    /// <summary>
    ///     Notes or reasons for rejection (if applicable).
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    ///     Admin or system user who reviewed/approved the verification.
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    ///     When the verification was reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    ///     Document IDs associated with this verification (if applicable).
    /// </summary>
    [MaxLength(2000)]
    public string? DocumentIds { get; set; }

    /// <summary>
    ///     Additional metadata about the verification (JSON).
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Gets whether the verification is currently valid.
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (Status != "Approved") { return false; }

            return !ExpiresAt.HasValue || ExpiresAt.Value > SystemClock.UtcNow;
        }
    }

    /// <summary>
    ///     Gets whether the verification is pending review.
    /// </summary>
    public bool IsPending { get => Status == "Pending"; }
}
