namespace GameGuild.Resources;

/// <summary>
///     Request DTO for archiving old resource usage records
/// </summary>
public class ArchiveResourceUsageRecordsRequest
{
    public DateTime OlderThan { get; set; }
}
