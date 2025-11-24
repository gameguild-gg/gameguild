using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record GenerateAccessReviewReportCommand : ICommand<AccessReviewReportDto>
{
    public Guid CampaignId { get; init; }

    public string ReportFormat { get; init; } = "PDF";
}
