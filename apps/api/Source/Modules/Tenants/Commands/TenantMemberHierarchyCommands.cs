using GameGuild.Core.Cqrs;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Command to assign a parent member to a tenant member
/// </summary>
public sealed record AssignParentMemberCommand(
    Guid MemberId,
    Guid ParentMemberId) : ICommand<Result>;

/// <summary>
///     Command to remove the parent assignment from a tenant member
/// </summary>
public sealed record RemoveParentMemberCommand(
    Guid MemberId) : ICommand<Result>;

/// <summary>
///     Command to move a member to a different position in the hierarchy
/// </summary>
public sealed record MoveMemberInHierarchyCommand(
    Guid MemberId,
    Guid? NewParentId) : ICommand<Result>;
