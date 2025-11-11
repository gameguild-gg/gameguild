using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

public record GetAccessReviewItemDetailsQuery : IQuery<AccessReviewItemDetails>
{
    public Guid ItemId { get; init; }
}
