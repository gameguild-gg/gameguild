using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for RecordRevenueEventCommand
/// </summary>
public sealed class RecordRevenueEventCommandHandler(IRevenueAuditService revenueAuditService) : ICommandHandler<RecordRevenueEventCommand, RevenueEvent>
{
    public async Task<RevenueEvent> Handle(RecordRevenueEventCommand request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.RecordRevenueEventAsync(request.EventType, request.Amount, request.Currency, request.Source, request.ReferenceId, request.UserId, request.Metadata, cancellationToken);
    }
}
