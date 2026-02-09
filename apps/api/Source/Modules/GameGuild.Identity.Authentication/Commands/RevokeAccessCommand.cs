using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record RevokeAccessCommand : ICommand<bool>
{
    public Guid UserId { get; init; }

    public Guid ResourceId { get; init; }

    public string ResourceType { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public Guid RevokedBy { get; init; }
}
