namespace GameGuild.Resources.Commands;

/// <summary>
///     Request DTO for archiving old resource usage records
/// </summary>
public class ArchiveResourceUsageRecordsRequest
{
    public DateTime OlderThan { get; set; }
}
