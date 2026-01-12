using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetAccessReviewItemDetailsQuery : IQuery<AccessReviewItemDetails>
{
    public Guid ItemId { get; init; }
}
