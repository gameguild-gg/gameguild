using GameGuild.Database;
using GameGuild.CQRS;
using GameGuild.Modules.Resources.Commands;
using GameGuild.Database;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Handlers;

/// <summary>
/// Handler for ArchiveUsageRecordsCommand
/// </summary>
public class ArchiveUsageRecordsHandler(
    ApplicationDbContext context,
    ILogger<ArchiveUsageRecordsHandler> logger)
    : IRequestHandler<ArchiveUsageRecordsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        ArchiveUsageRecordsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Archiving usage records older than {OlderThan}", request.OlderThan);

            // Delete old usage records
            var count = await context.Set<ResourceUsageRecord>()
                .Where(r => r.RecordedAt < request.OlderThan)
                .ExecuteDeleteAsync(cancellationToken);

            logger.LogInformation("Archived {Count} usage records", count);

            return Result.Success(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error archiving usage records older than {OlderThan}", request.OlderThan);
            return Result.Failure<int>($"Failed to archive usage records: {ex.Message}");
        }
    }
}
