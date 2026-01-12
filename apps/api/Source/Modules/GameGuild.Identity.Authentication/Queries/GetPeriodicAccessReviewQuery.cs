using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetPeriodicAccessReviewQuery : IQuery<PeriodicAccessReview>
{
    public Guid ReviewId { get; init; }
}
