using GameGuild.CQRS;

namespace GameGuild.Authentication.DTOs.Queries;

public record GetUserPermissionsQuery : IQuery<UserPermissionsDto>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }
}
