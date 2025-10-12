using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Handler for GetDisputesByUserIdQuery</summary>
public class GetDisputesByUserIdHandler : IRequestHandler<GetDisputesByUserIdQuery, List<PaymentDispute>>
{
    private readonly IDisputeService _disputeService;

    public GetDisputesByUserIdHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<List<PaymentDispute>> Handle(GetDisputesByUserIdQuery request, CancellationToken cancellationToken)
    {
        return await _disputeService.GetDisputesByUserIdAsync(
            request.UserId,
            request.Skip,
            request.Take,
            cancellationToken);
    }
}
