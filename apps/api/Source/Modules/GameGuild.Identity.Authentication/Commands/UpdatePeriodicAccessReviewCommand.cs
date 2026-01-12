using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record UpdatePeriodicAccessReviewCommand : ICommand<PeriodicAccessReview>
{
    public Guid ReviewId { get; set; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Schedule { get; init; }

    public bool? IsActive { get; init; }
}
