using GameGuild.Authentication.Entities;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ReviewAccessItemCommand : ICommand<AccessReviewItem>
{
    public Guid ItemId { get; init; }

    public string Decision { get; init; } = string.Empty;

    public string? Justification { get; init; }

    public Guid ReviewerId { get; init; }
}
