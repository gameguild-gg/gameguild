namespace GameGuild.Identity.Authentication;

public abstract class AccessRevocationRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public Guid RevokedBy { get; set; }

    public string RevokedByName { get; set; } = string.Empty;

    public DateTime RevokedAt { get; set; }
}
