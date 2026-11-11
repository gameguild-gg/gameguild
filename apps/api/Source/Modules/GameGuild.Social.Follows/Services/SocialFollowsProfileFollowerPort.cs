using GameGuild.Social.Profiles;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Follows.Services;

/// <summary>
///     Social.Profiles' follow-graph port (issue #1315 SP1): serves the explicit
///     follower entitlement for Private/Connections profile reads. Registered by
///     <see cref="FollowsModule.AddFollowsModule"/> — it overrides the fail-closed
///     <see cref="NullProfileFollowerPort"/> default installed by the Profiles module.
/// </summary>
public sealed class SocialFollowsProfileFollowerPort(IApplicationDbContext context) : IProfileFollowerPort
{
    public async Task<bool> IsFollowerAsync(Guid followerUserId, Guid profileUserId, CancellationToken ct = default)
        => await context.Set<Follow>()
            .AsNoTracking()
            .AnyAsync(follow => follow.FollowerId == followerUserId
                && follow.FollowedEntityId == profileUserId
                && follow.FollowedEntityType == FollowableEntityTypes.User
                && follow.DeletedAt == null, ct)
            .ConfigureAwait(false);
}
