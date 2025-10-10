using GameGuild.Messaging;

namespace GameGuild.Modules.Resources.Commands;

/// <summary>
/// Command to archive old usage records
/// </summary>
/// <param name="OlderThan">Archive records older than this date</param>
public record ArchiveUsageRecordsCommand(DateTime OlderThan) : IRequest<Result<int>>;
