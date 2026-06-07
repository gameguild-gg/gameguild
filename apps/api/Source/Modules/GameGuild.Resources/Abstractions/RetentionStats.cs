namespace GameGuild.Resources;

/// <summary>
///     Retention statistics
/// </summary>
public class RetentionStats
{
    public int TotalRecords { get; set; }

    public int ArchivedRecords { get; set; }

    public int ActiveRecords { get; set; }

    public long TotalStorageBytes { get; set; }

    public long ArchivedStorageBytes { get; set; }

    public DateTime? OldestRecordDate { get; set; }
}
