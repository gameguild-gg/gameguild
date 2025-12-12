using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record SendReviewRemindersCommand : ICommand<ReminderResult>
{
    public Guid CampaignId { get; init; }
}
