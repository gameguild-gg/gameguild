namespace GameGuild.Identity.Authentication;

public abstract class BulkPermissionResult
{
    public int TotalRequested { get; set; }

    public int Successful { get; set; }

    public int Failed { get; set; }

    public List<BulkPermissionFailure> Failures { get; set; } = new List<BulkPermissionFailure>();

    public DateTime ProcessedAt { get; set; }
}
