using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetPeriodicAccessReviewQuery : IQuery<PeriodicAccessReview>
{
    public Guid ReviewId { get; init; }
}
