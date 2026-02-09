using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetAccessReviewItemDetailsQuery : IQuery<AccessReviewItemDetails>
{
    public Guid ItemId { get; init; }
}
