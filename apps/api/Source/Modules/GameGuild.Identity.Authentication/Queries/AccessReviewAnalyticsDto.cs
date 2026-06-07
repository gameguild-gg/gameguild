namespace GameGuild.Identity.Authentication;

public abstract class AccessReviewAnalyticsDto
{
    public int TotalCampaigns { get; set; }

    public int ActiveCampaigns { get; set; }

    public int CompletedCampaigns { get; set; }

    public int TotalReviewItems { get; set; }

    public int ApprovedItems { get; set; }

    public int RevokedItems { get; set; }

    public int PendingItems { get; set; }

    public double AverageCompletionTime { get; set; }

    public Dictionary<string, int> ReviewsByType { get; set; } = new Dictionary<string, int>();
}
