using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Handler for GetDisputeByIdQuery</summary>
public class GetDisputeByIdHandler : IRequestHandler<GetDisputeByIdQuery, PaymentDispute?>
{
    private readonly IDisputeService _disputeService;

    public GetDisputeByIdHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<PaymentDispute?> Handle(GetDisputeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _disputeService.GetDisputeByIdAsync(request.DisputeId, cancellationToken);
    }
}
