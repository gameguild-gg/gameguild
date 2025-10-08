using GameGuild.Core;
using MediatR;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command to initiate KYC verification
/// </summary>
public record InitiateKycVerificationCommand(
    Guid UserId,
    string Provider,
    VerificationLevel Level,
    VerificationType Type,
    string? InitiatedFromIp = null
) : IRequest<Result<Guid>>;

/// <summary>
/// Command to update KYC verification status
/// </summary>
public record UpdateKycVerificationStatusCommand(
    Guid VerificationId,
    VerificationStatus Status,
    string? ResultDetails = null,
    int? RiskScore = null,
    string? RejectionReason = null
) : IRequest<Result>;

/// <summary>
/// Command to complete KYC verification
/// </summary>
public record CompleteKycVerificationCommand(
    Guid VerificationId,
    bool Approved,
    string? ResultDetails = null,
    int? RiskScore = null,
    string? RejectionReason = null,
    TimeSpan? ValidityPeriod = null
) : IRequest<Result>;

/// <summary>
/// Command to update compliance flags
/// </summary>
public record UpdateComplianceFlagsCommand(
    Guid VerificationId,
    ComplianceFlags Flags
) : IRequest<Result>;
