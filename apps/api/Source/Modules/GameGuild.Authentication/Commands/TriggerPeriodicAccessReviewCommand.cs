using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record TriggerPeriodicAccessReviewCommand : ICommand<AccessReviewCampaign>
{
    public Guid ReviewId { get; init; }
}
