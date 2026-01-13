using GameGuild.Models;

namespace GameGuild.Compliance.KYC;

public interface IKycService
{
    /// <summary>
    /// Submit a new KYC verification request
    /// </summary>
    Task<Result<UserKycVerification>> SubmitVerificationAsync(
        Guid userId,
        KycProvider provider,
        string verificationLevel,
        string documentTypes,
        string? documentCountry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the status of a KYC verification
    /// </summary>
    Task<Result<UserKycVerification>> UpdateVerificationStatusAsync(
        Guid verificationId,
        KycVerificationStatus status,
        string? notes,
        DateTime? completedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get verification by ID
    /// </summary>
    Task<Result<UserKycVerification>> GetVerificationByIdAsync(
        Guid verificationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all verifications for a user
    /// </summary>
    Task<Result<List<UserKycVerification>>> GetVerificationsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest active verification for a user
    /// </summary>
    Task<Result<UserKycVerification?>> GetLatestVerificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has an approved verification
    /// </summary>
    Task<Result<bool>> IsUserVerifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get verifications by status
    /// </summary>
    Task<Result<List<UserKycVerification>>> GetVerificationsByStatusAsync(
        KycVerificationStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload verification document
    /// </summary>
    Task<Result<string>> UploadDocumentAsync(
        Guid verificationId,
        string documentType,
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process provider webhook callback
    /// </summary>
    Task<Result<bool>> ProcessProviderWebhookAsync(
        KycProvider provider,
        string externalVerificationId,
        KycVerificationStatus status,
        string? providerData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get compliance report
    /// </summary>
    Task<Result<KycComplianceReportDto>> GetComplianceReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

public class KycComplianceReportDto
{
    public int TotalVerifications { get; set; }
    public int ApprovedVerifications { get; set; }
    public int RejectedVerifications { get; set; }
    public int PendingVerifications { get; set; }
    public int ExpiredVerifications { get; set; }
    public Dictionary<KycProvider, int> VerificationsByProvider { get; set; } = new();
    public Dictionary<string, int> VerificationsByCountry { get; set; } = new();
    public double ApprovalRate { get; set; }
}
