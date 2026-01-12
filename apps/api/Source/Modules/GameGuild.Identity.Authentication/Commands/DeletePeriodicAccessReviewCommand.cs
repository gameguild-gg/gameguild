using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record DeletePeriodicAccessReviewCommand : ICommand<bool>
{
    public Guid ReviewId { get; init; }
}
