using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetUserPermissionsQuery : IQuery<UserPermissionsDto>
{
    public Guid UserId { get; init; }

    public Guid? TenantId { get; init; }
}
