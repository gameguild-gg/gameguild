using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.KYC;

public class KycService : IKycService
{
    private readonly IKycRepository _repository;
    private readonly ILogger<KycService> _logger;

    public KycService(IKycRepository repository, ILogger<KycService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<UserKycVerification>> SubmitVerificationAsync(
        Guid userId,
        KycProvider provider,
        string verificationLevel,
        string documentTypes,
        string? documentCountry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verification = new UserKycVerification
            {
                UserId = userId,
                Provider = provider,
                Status = KycVerificationStatus.Pending,
                VerificationLevel = verificationLevel,
                DocumentTypes = documentTypes,
                DocumentCountry = documentCountry,
                SubmittedAt = SystemClock.UtcNow
            };

            await _repository.CreateAsync(verification, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("KYC verification submitted for user {UserId} with provider {Provider}", userId, provider);

            return Result<UserKycVerification>.Success(verification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit KYC verification for user {UserId}", userId);
            return Result.Failure<UserKycVerification>(Error.Failure("KYC.SubmitFailed", $"Failed to submit KYC verification: {ex.Message}"));
        }
    }

    public async Task<Result<UserKycVerification>> UpdateVerificationStatusAsync(
        Guid verificationId,
        KycVerificationStatus status,
        string? notes,
        DateTime? completedAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verification = await _repository.GetByIdAsync(verificationId, cancellationToken).ConfigureAwait(false);
            if (verification == null)
            {
                return Result.Failure<UserKycVerification>(Error.NotFound("KYC.NotFound", "Verification not found"));
            }

            verification.Status = status;
            verification.Notes = notes;
            verification.CompletedAt = completedAt ?? (status == KycVerificationStatus.Approved || status == KycVerificationStatus.Rejected ? SystemClock.UtcNow : null);

            if (status == KycVerificationStatus.Approved)
            {
                verification.ExpiresAt = SystemClock.UtcNow.AddYears(1); // Set expiration to 1 year
            }

            await _repository.UpdateAsync(verification, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("KYC verification {VerificationId} status updated to {Status}", verificationId, status);

            return Result<UserKycVerification>.Success(verification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update KYC verification status for {VerificationId}", verificationId);
            return Result.Failure<UserKycVerification>(Error.Failure("KYC.UpdateFailed", $"Failed to update verification status: {ex.Message}"));
        }
    }

    public async Task<Result<UserKycVerification>> GetVerificationByIdAsync(
        Guid verificationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verification = await _repository.GetByIdAsync(verificationId, cancellationToken).ConfigureAwait(false);
            if (verification == null)
            {
                return Result.Failure<UserKycVerification>(Error.NotFound("KYC.NotFound", "Verification not found"));
            }

            return Result<UserKycVerification>.Success(verification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get KYC verification {VerificationId}", verificationId);
            return Result.Failure<UserKycVerification>(Error.Failure("KYC.GetFailed", $"Failed to get verification: {ex.Message}"));
        }
    }

    public async Task<Result<List<UserKycVerification>>> GetVerificationsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verifications = await _repository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
            return Result<List<UserKycVerification>>.Success(verifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get KYC verifications for user {UserId}", userId);
            return Result.Failure<List<UserKycVerification>>(Error.Failure("KYC.GetFailed", $"Failed to get verifications: {ex.Message}"));
        }
    }

    public async Task<Result<UserKycVerification?>> GetLatestVerificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verification = await _repository.GetLatestVerificationAsync(userId, cancellationToken).ConfigureAwait(false);
            return Result<UserKycVerification?>.Success(verification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest KYC verification for user {UserId}", userId);
            return Result.Failure<UserKycVerification?>(Error.Failure("KYC.GetFailed", $"Failed to get latest verification: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> IsUserVerifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isVerified = await _repository.HasApprovedVerificationAsync(userId, cancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(isVerified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is verified", userId);
            return Result.Failure<bool>(Error.Failure("KYC.CheckFailed", $"Failed to check verification status: {ex.Message}"));
        }
    }

    public async Task<Result<List<UserKycVerification>>> GetVerificationsByStatusAsync(
        KycVerificationStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verifications = await _repository.GetByStatusAsync(status, cancellationToken).ConfigureAwait(false);
            return Result<List<UserKycVerification>>.Success(verifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get KYC verifications by status {Status}", status);
            return Result.Failure<List<UserKycVerification>>(Error.Failure("KYC.GetFailed", $"Failed to get verifications: {ex.Message}"));
        }
    }

    public async Task<Result<string>> UploadDocumentAsync(
        Guid verificationId,
        string documentType,
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verification = await _repository.GetByIdAsync(verificationId, cancellationToken).ConfigureAwait(false);
            if (verification == null)
            {
                return Result.Failure<string>(Error.NotFound("KYC.NotFound", "Verification not found"));
            }

            // PLANNED: Implement actual document storage (S3, Azure Blob, etc.) (depends on GameGuild.Storage)
            var documentUrl = $"kyc-documents/{verificationId}/{fileName}";

            // Update verification with document type
            if (string.IsNullOrEmpty(verification.DocumentTypes))
            {
                verification.DocumentTypes = documentType;
            }
            else if (!verification.DocumentTypes.Contains(documentType))
            {
                verification.DocumentTypes += $",{documentType}";
            }

            await _repository.UpdateAsync(verification, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Document {DocumentType} uploaded for verification {VerificationId}", documentType, verificationId);

            return Result<string>.Success(documentUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document for verification {VerificationId}", verificationId);
            return Result.Failure<string>(Error.Failure("KYC.UploadFailed", $"Failed to upload document: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> ProcessProviderWebhookAsync(
        KycProvider provider,
        string externalVerificationId,
        KycVerificationStatus status,
        string? providerData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verification = await _repository.GetByExternalIdAsync(externalVerificationId, cancellationToken).ConfigureAwait(false);
            if (verification == null)
            {
                _logger.LogWarning("Verification not found for external ID {ExternalId}", externalVerificationId);
                return Result.Failure<bool>(Error.NotFound("KYC.NotFound", "Verification not found"));
            }

            verification.Status = status;
            verification.ProviderData = providerData;
            verification.CompletedAt = status == KycVerificationStatus.Approved || status == KycVerificationStatus.Rejected
                ? SystemClock.UtcNow
                : null;

            if (status == KycVerificationStatus.Approved)
            {
                verification.ExpiresAt = SystemClock.UtcNow.AddYears(1);
            }

            await _repository.UpdateAsync(verification, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Provider webhook processed for verification {VerificationId}", verification.Id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process provider webhook for external ID {ExternalId}", externalVerificationId);
            return Result.Failure<bool>(Error.Failure("KYC.WebhookFailed", $"Failed to process webhook: {ex.Message}"));
        }
    }

    public async Task<Result<KycComplianceReportDto>> GetComplianceReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var verifications = await _repository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);

            var report = new KycComplianceReportDto
            {
                TotalVerifications = verifications.Count,
                ApprovedVerifications = verifications.Count(v => v.Status == KycVerificationStatus.Approved),
                RejectedVerifications = verifications.Count(v => v.Status == KycVerificationStatus.Rejected),
                PendingVerifications = verifications.Count(v => v.Status == KycVerificationStatus.Pending),
                ExpiredVerifications = verifications.Count(v => v.Status == KycVerificationStatus.Expired),
                VerificationsByProvider = verifications
                    .GroupBy(v => v.Provider)
                    .ToDictionary(g => g.Key, g => g.Count()),
                VerificationsByCountry = verifications
                    .Where(v => !string.IsNullOrEmpty(v.DocumentCountry))
                    .GroupBy(v => v.DocumentCountry!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ApprovalRate = verifications.Count > 0
                    ? (double)verifications.Count(v => v.Status == KycVerificationStatus.Approved) / verifications.Count * 100
                    : 0
            };

            return Result<KycComplianceReportDto>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            return Result.Failure<KycComplianceReportDto>(Error.Failure("KYC.ReportFailed", $"Failed to generate report: {ex.Message}"));
        }
    }
}
