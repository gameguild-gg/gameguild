using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Query to get all tenant memberships for a user.
///     Similar to Discord's "My Servers" - shows all tenants the user belongs to.
/// </summary>
/// <param name="UserId">The user ID to get memberships for</param>
/// <param name="IncludeInactive">Whether to include inactive memberships (default: false)</param>
public record GetUserMembershipsQuery(Guid UserId, bool IncludeInactive = false) : IQuery<GetUserMembershipsResponse>;
