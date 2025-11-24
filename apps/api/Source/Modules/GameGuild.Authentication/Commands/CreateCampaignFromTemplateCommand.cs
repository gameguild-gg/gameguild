using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record CreateCampaignFromTemplateCommand : ICommand<AccessReviewCampaign>
{
    public Guid TemplateId { get; init; }

    public Guid TenantId { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }
}
