using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to archive old resource usage records
/// </summary>
/// <param name="OlderThan">Archive records older than this date</param>
public sealed record ArchiveResourceUsageRecordsCommand(DateTime OlderThan) : ICommand<int>;
