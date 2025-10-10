using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Command to add evidence to a dispute</summary>
public record AddDisputeEvidenceCommand(
    Guid DisputeId,
    EvidenceType EvidenceType,
    string Title,
    string Description,
    Guid SubmittedBy,
    bool IsFromMerchant = false,
    string? FileUrl = null,
    string? FileName = null,
    long? FileSize = null,
    string? MimeType = null
) : IRequest<DisputeEvidence>;
