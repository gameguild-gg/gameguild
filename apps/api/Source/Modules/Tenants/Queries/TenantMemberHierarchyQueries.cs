using GameGuild.Core.Cqrs;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Query to get direct children of a member
/// </summary>
public sealed record GetMemberChildrenQuery(
    Guid MemberId) : IQuery<Result<IReadOnlyList<TenantMemberDto>>>;

/// <summary>
///     Query to get the complete hierarchy for a member (all descendants)
/// </summary>
public sealed record GetMemberHierarchyQuery(
    Guid MemberId) : IQuery<Result<IReadOnlyList<TenantMemberDto>>>;

/// <summary>
///     Query to get the entire tenant hierarchy tree (all members with relationships)
/// </summary>
public sealed record GetTenantHierarchyTreeQuery(
    Guid TenantId) : IQuery<Result<IReadOnlyList<TenantMemberDto>>>;
