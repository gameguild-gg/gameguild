using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record BulkReviewAccessItemsCommand : ICommand<BulkAccessReviewResult>
{
    public List<Guid> ItemIds { get; init; } = new List<Guid>();

    public string Decision { get; init; } = string.Empty;

    public string? Justification { get; init; }

    public Guid ReviewerId { get; init; }
}
