using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record ClearPermissionCacheCommand : ICommand<bool>
{
    public Guid? UserId { get; init; }

    public Guid? TenantId { get; init; }
}
