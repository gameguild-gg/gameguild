using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

public record GetPeriodicAccessReviewQuery : IQuery<PeriodicAccessReview>
{
    public Guid ReviewId { get; init; }
}
