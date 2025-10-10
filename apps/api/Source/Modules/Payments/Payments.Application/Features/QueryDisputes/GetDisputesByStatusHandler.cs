using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Handler for GetDisputesByStatusQuery</summary>
public class GetDisputesByStatusHandler : IRequestHandler<GetDisputesByStatusQuery, List<PaymentDispute>>
{
    private readonly IDisputeService _disputeService;

    public GetDisputesByStatusHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<List<PaymentDispute>> Handle(GetDisputesByStatusQuery request, CancellationToken cancellationToken)
    {
        return await _disputeService.GetDisputesByStatusAsync(
            request.Status,
            request.Skip,
            request.Take,
            cancellationToken);
    }
}
