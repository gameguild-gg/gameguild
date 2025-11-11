using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Handler for archiving old resource usage records
/// </summary>
public class ArchiveResourceUsageRecordsCommandHandler(IUsageRecordRepository usageRecordRepository) : ICommandHandler<ArchiveResourceUsageRecordsCommand, int>
{
    public async Task<int> Handle(ArchiveResourceUsageRecordsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var archivedCount = await usageRecordRepository.ArchiveOlderThanAsync(request.OlderThan, cancellationToken).ConfigureAwait(false);

        return archivedCount;
    }
}
