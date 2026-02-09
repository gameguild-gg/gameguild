using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GenerateAccessReviewReportCommand : ICommand<AccessReviewReportDto>
{
    public Guid CampaignId { get; init; }

    public string ReportFormat { get; init; } = "PDF";
}
