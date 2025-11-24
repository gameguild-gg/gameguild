using GameGuild.Authentication.Enums;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record RevokeResourcePermissionCommand : ICommand<bool>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public Guid ResourceId { get; init; }

    public List<PermissionType> Permissions { get; init; } = new List<PermissionType>();

    public string? RevokedBy { get; init; }

    public string? Reason { get; init; }
}
