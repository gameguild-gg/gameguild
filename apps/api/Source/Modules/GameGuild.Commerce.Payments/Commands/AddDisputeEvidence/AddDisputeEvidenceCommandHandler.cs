using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for AddDisputeEvidenceCommand
/// </summary>
public sealed class AddDisputeEvidenceCommandHandler(IDisputeService disputeService) : ICommandHandler<AddDisputeEvidenceCommand, DisputeEvidence>
{
    public async Task<DisputeEvidence> Handle(AddDisputeEvidenceCommand request, CancellationToken cancellationToken)
    {
        return await disputeService.AddEvidenceAsync(
            request.DisputeId,
            request.EvidenceType,
            request.Title,
            request.Description,
            request.SubmittedBy,
            request.IsFromMerchant,
            request.FileUrl,
            request.FileName,
            request.FileSize,
            request.MimeType,
            cancellationToken
        ).ConfigureAwait(false);
    }
}
