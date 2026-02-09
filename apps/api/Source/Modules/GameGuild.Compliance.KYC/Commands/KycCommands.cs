using GameGuild.CQRS;

namespace GameGuild.Compliance.KYC;

// Commands
public sealed record SubmitKycVerificationCommand(
    Guid UserId,
    KycProvider Provider,
    string VerificationLevel,
    string DocumentTypes,
    string? DocumentCountry
) : IRequest<Result<UserKycVerification>>;

public sealed record UpdateKycVerificationStatusCommand(
    Guid VerificationId,
    KycVerificationStatus Status,
    string? Notes,
    DateTime? CompletedAt
) : IRequest<Result<UserKycVerification>>;

public sealed record UploadKycDocumentCommand(
    Guid VerificationId,
    string DocumentType,
    Stream DocumentStream,
    string FileName
) : IRequest<Result<string>>;

public sealed record ProcessKycProviderWebhookCommand(
    KycProvider Provider,
    string ExternalVerificationId,
    KycVerificationStatus Status,
    string? ProviderData
) : IRequest<Result<bool>>;

public sealed record DeleteKycVerificationCommand(
    Guid VerificationId
) : IRequest<Result<bool>>;

// Queries
public sealed record GetKycVerificationByIdQuery(
    Guid VerificationId
) : IRequest<Result<UserKycVerification>>;

public sealed record GetKycVerificationsByUserIdQuery(
    Guid UserId
) : IRequest<Result<List<UserKycVerification>>>;

public sealed record GetLatestKycVerificationQuery(
    Guid UserId
) : IRequest<Result<UserKycVerification?>>;

public sealed record IsUserVerifiedQuery(
    Guid UserId
) : IRequest<Result<bool>>;

public sealed record GetKycVerificationsByStatusQuery(
    KycVerificationStatus Status
) : IRequest<Result<List<UserKycVerification>>>;

public sealed record GetKycComplianceReportQuery(
    DateTime StartDate,
    DateTime EndDate
) : IRequest<Result<KycComplianceReportDto>>;
