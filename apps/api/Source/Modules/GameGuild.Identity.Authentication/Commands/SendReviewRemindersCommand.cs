using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record SendReviewRemindersCommand : ICommand<ReminderResult>
{
    public Guid CampaignId { get; init; }
}
