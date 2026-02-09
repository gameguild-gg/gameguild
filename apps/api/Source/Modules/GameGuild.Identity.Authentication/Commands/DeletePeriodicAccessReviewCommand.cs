using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record DeletePeriodicAccessReviewCommand : ICommand<bool>
{
    public Guid ReviewId { get; init; }
}
