using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for archiving old resource usage records
/// </summary>
public sealed class ArchiveResourceUsageRecordsCommandHandler(IUsageRecordRepository usageRecordRepository) : ICommandHandler<ArchiveResourceUsageRecordsCommand, int>
{
    public async Task<int> Handle(ArchiveResourceUsageRecordsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var archivedCount = await usageRecordRepository.ArchiveOlderThanAsync(request.OlderThan, cancellationToken).ConfigureAwait(false);

        return archivedCount;
    }
}
