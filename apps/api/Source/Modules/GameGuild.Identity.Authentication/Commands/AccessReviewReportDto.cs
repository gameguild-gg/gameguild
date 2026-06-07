namespace GameGuild.Identity.Authentication;

public abstract class AccessReviewReportDto
{
    public Guid ReportId { get; set; }

    public Guid CampaignId { get; set; }

    public string ReportUrl { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }
}
