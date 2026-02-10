using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for RecordAuditTrailCommand
/// </summary>
public sealed class RecordAuditTrailCommandHandler(IRevenueAuditService revenueAuditService) : ICommandHandler<RecordAuditTrailCommand>
{
    public async Task<Unit> Handle(RecordAuditTrailCommand request, CancellationToken cancellationToken)
    {
        await revenueAuditService.RecordAuditTrailAsync(
            request.EntityType,
            request.EntityId,
            request.Action,
            request.ChangedBy,
            request.OldValue,
            request.NewValue,
            request.IpAddress,
            request.UserAgent,
            request.Reason,
            cancellationToken
        ).ConfigureAwait(false);

        return Unit.Value;
    }
}
