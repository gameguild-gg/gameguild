namespace GameGuild.Resources;

/// <summary>
///     Result of executing a retention policy
/// </summary>
public class RetentionExecutionResult
{
    public int RecordsArchived { get; set; }

    public int RecordsDeleted { get; set; }

    public int RecordsCompacted { get; set; }

    public long BytesFreed { get; set; }

    public DateTime ExecutedAt { get; set; } = SystemClock.UtcNow;
}
