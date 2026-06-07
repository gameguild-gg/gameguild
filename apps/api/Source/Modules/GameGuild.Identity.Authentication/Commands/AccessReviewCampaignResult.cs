namespace GameGuild.Identity.Authentication;

public abstract class AccessReviewCampaignResult
{
    public Guid CampaignId { get; set; }

    public int TotalItems { get; set; }

    public int ReviewedItems { get; set; }

    public int ApprovedItems { get; set; }

    public int RevokedItems { get; set; }

    public int PendingItems { get; set; }

    public DateTime CompletedAt { get; set; }
}
