namespace GameGuild.Identity.Authentication;

public abstract class BulkAccessReviewResult
{
    public int TotalItems { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public List<string> Errors { get; set; } = new List<string>();
}
