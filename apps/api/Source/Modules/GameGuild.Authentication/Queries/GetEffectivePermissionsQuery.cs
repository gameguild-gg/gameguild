using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetEffectivePermissionsQuery : IQuery<EffectivePermissionsDto>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public Guid? ResourceId { get; init; }

    public string? ContentType { get; init; }
}
