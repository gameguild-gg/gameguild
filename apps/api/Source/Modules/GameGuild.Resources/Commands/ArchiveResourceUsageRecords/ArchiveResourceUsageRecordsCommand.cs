using GameGuild.CQRS;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Command to archive old resource usage records
/// </summary>
/// <param name="OlderThan">Archive records older than this date</param>
public record ArchiveResourceUsageRecordsCommand(DateTime OlderThan) : ICommand<int>;
