using GameGuild.CQRS;
using GameGuild.Common;
using GameGuild.Modules.Kyc.Models;
using GameGuild.Modules.Kyc.Services;

namespace GameGuild.Modules.Kyc.Commands;

// Commands
public record SubmitKycVerificationCommand(
    Guid UserId,
    KycProvider Provider,
    string VerificationLevel,
    string DocumentTypes,
    string? DocumentCountry
) : IRequest<Result<UserKycVerification>>;

public record UpdateKycVerificationStatusCommand(
    Guid VerificationId,
    KycVerificationStatus Status,
    string? Notes,
    DateTime? CompletedAt
) : IRequest<Result<UserKycVerification>>;

public record UploadKycDocumentCommand(
    Guid VerificationId,
    string DocumentType,
    Stream DocumentStream,
    string FileName
) : IRequest<Result<string>>;

public record ProcessKycProviderWebhookCommand(
    KycProvider Provider,
    string ExternalVerificationId,
    KycVerificationStatus Status,
    string? ProviderData
) : IRequest<Result<bool>>;

public record DeleteKycVerificationCommand(
    Guid VerificationId
) : IRequest<Result<bool>>;

// Queries
public record GetKycVerificationByIdQuery(
    Guid VerificationId
) : IRequest<Result<UserKycVerification>>;

public record GetKycVerificationsByUserIdQuery(
    Guid UserId
) : IRequest<Result<List<UserKycVerification>>>;

public record GetLatestKycVerificationQuery(
    Guid UserId
) : IRequest<Result<UserKycVerification?>>;

public record IsUserVerifiedQuery(
    Guid UserId
) : IRequest<Result<bool>>;

public record GetKycVerificationsByStatusQuery(
    KycVerificationStatus Status
) : IRequest<Result<List<UserKycVerification>>>;

public record GetKycComplianceReportQuery(
    DateTime StartDate,
    DateTime EndDate
) : IRequest<Result<KycComplianceReportDto>>;
