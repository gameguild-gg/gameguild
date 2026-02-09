using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetEffectivePermissionsQuery : IQuery<EffectivePermissionsDto>
{
    public Guid UserId { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? ResourceId { get; init; }

    public string? ContentType { get; init; }
}
