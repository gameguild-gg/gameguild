using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record CreatePeriodicAccessReviewCommand : ICommand<PeriodicAccessReview>
{
    public Guid TenantId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string Schedule { get; init; } = string.Empty;

    public string ReviewType { get; init; } = string.Empty;

    public List<Guid> ReviewerIds { get; init; } = new List<Guid>();
}
