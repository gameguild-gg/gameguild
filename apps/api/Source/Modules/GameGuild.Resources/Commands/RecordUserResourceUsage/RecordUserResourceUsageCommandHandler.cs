using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for recording user resource usage
/// </summary>
public sealed class RecordUserResourceUsageCommandHandler(IUsageRecordRepository usageRecordRepository) : ICommandHandler<RecordUserResourceUsageCommand, Guid>
{
    public async Task<Guid> Handle(RecordUserResourceUsageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = new UsageRecord
        {
            UserId = request.UserId,
            Type = request.ResourceUsageType,
            Count = request.Count,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Metadata = request.Metadata
        };

        await usageRecordRepository.CreateAsync(record, cancellationToken).ConfigureAwait(false);

        return record.Id;
    }
}
