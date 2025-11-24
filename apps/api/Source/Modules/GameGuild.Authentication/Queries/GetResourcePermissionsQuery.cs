using GameGuild.Authentication.Enums;
using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetResourcePermissionsQuery : IQuery<IEnumerable<PermissionType>>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public Guid ResourceId { get; init; }
}
