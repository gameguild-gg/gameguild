using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record DeletePeriodicAccessReviewCommand : ICommand<bool>
{
    public Guid ReviewId { get; init; }
}
