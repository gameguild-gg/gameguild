namespace GameGuild.Identity.Authentication;

public abstract class AccessReviewItemDetails
{
    public Guid ItemId { get; set; }

    public Guid CampaignId { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public List<string> CurrentPermissions { get; set; } = new List<string>();

    public string Status { get; set; } = string.Empty;

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewerDecision { get; set; }

    public string? Justification { get; set; }
}
