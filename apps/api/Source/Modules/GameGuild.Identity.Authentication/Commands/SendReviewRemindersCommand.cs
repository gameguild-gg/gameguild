using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record SendReviewRemindersCommand : ICommand<ReminderResult>
{
    public Guid CampaignId { get; init; }
}
