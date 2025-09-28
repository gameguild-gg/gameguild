using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Event raised when a token is refreshed - for logging and analytics </summary>
public sealed class TokenRefreshedEvent(Guid userId, string email, Guid? tenantId, DateTime refreshedAt) : DomainEventBase(userId, nameof(RefreshToken))
{
    public Guid UserId { get; } = userId;

    public string Email { get; } = email;

    public Guid? TenantId { get; } = tenantId;

    public DateTime RefreshedAt { get; } = refreshedAt;
}
