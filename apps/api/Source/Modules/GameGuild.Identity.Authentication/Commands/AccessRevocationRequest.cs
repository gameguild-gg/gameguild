namespace GameGuild.Identity.Authentication;

public abstract record AccessRevocationRequest
{
    public Guid UserId { get; init; }

    public Guid ResourceId { get; init; }

    public string ResourceType { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;
}
