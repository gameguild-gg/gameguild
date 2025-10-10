using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Handler for AddDisputeEvidenceCommand</summary>
public class AddDisputeEvidenceHandler : IRequestHandler<AddDisputeEvidenceCommand, DisputeEvidence>
{
    private readonly IDisputeService _disputeService;

    public AddDisputeEvidenceHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<DisputeEvidence> Handle(AddDisputeEvidenceCommand request, CancellationToken cancellationToken)
    {
        return await _disputeService.AddEvidenceAsync(
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
            cancellationToken);
    }
}
