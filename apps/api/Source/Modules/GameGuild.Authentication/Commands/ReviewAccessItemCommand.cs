using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record ReviewAccessItemCommand : ICommand<AccessReviewItem>
{
    public Guid ItemId { get; init; }

    public string Decision { get; init; } = string.Empty;

    public string? Justification { get; init; }

    public Guid ReviewerId { get; init; }
}
