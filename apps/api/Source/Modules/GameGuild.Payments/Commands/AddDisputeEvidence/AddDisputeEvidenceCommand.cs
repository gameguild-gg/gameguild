using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to add evidence to a dispute
/// </summary>
public record AddDisputeEvidenceCommand(
    Guid DisputeId,
    EvidenceType EvidenceType,
    string Title,
    string Description,
    Guid SubmittedBy,
    bool IsFromMerchant,
    string? FileUrl = null,
    string? FileName = null,
    long? FileSize = null,
    string? MimeType = null
) : ICommand<DisputeEvidence>;
