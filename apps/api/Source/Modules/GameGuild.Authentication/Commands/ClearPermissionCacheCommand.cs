using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ClearPermissionCacheCommand : ICommand<bool>
{
    public Guid? UserId { get; init; }

    public Guid? TenantId { get; init; }
}
