using GameGuild.Core;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for assigning a parent member
/// </summary>
public sealed class AssignParentMemberHandler(
    ITenantMemberRepository repository,
    ILogger<AssignParentMemberHandler> logger) : IRequestHandler<AssignParentMemberCommand, Result>
{
    public async Task<Result> Handle(AssignParentMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the member
            var member = await repository.GetMemberAsync(request.MemberId, Guid.Empty, cancellationToken);
            if (member == null)
            {
                return Result.Failure($"Member with ID {request.MemberId} not found");
            }

            // Get the parent member
            var parentMember = await repository.GetMemberAsync(request.ParentMemberId, Guid.Empty, cancellationToken);
            if (parentMember == null)
            {
                return Result.Failure($"Parent member with ID {request.ParentMemberId} not found");
            }

            // Validate same tenant
            if (member.TenantId != parentMember.TenantId)
            {
                return Result.Failure("Parent member must be in the same tenant");
            }

            // Prevent circular reference
            if (parentMember.HierarchyPath != null &&
                parentMember.HierarchyPath.Contains(member.Id.ToString()))
            {
                return Result.Failure("Cannot assign parent: would create circular reference");
            }

            // Update member hierarchy
            member.ParentMemberId = request.ParentMemberId;
            var newHierarchyPath = parentMember.HierarchyPath != null
                ? $"{parentMember.HierarchyPath}/{member.Id}"
                : member.Id.ToString();

            await repository.UpdateHierarchyPathAsync(member.Id, newHierarchyPath, cancellationToken);

            logger.LogInformation("Assigned parent {ParentId} to member {MemberId}",
                request.ParentMemberId, request.MemberId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning parent member {ParentId} to {MemberId}",
                request.ParentMemberId, request.MemberId);
            return Result.Failure($"Failed to assign parent member: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for removing parent member assignment
/// </summary>
public sealed class RemoveParentMemberHandler(
    ITenantMemberRepository repository,
    ILogger<RemoveParentMemberHandler> logger) : IRequestHandler<RemoveParentMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveParentMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the member
            var member = await repository.GetMemberAsync(request.MemberId, Guid.Empty, cancellationToken);
            if (member == null)
            {
                return Result.Failure($"Member with ID {request.MemberId} not found");
            }

            if (member.ParentMemberId == null)
            {
                return Result.Failure("Member does not have a parent assigned");
            }

            // Update member hierarchy
            member.ParentMemberId = null;
            await repository.UpdateHierarchyPathAsync(member.Id, member.Id.ToString(), cancellationToken);

            logger.LogInformation("Removed parent from member {MemberId}", request.MemberId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing parent from member {MemberId}", request.MemberId);
            return Result.Failure($"Failed to remove parent member: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for moving a member in the hierarchy
/// </summary>
public sealed class MoveMemberInHierarchyHandler(
    ITenantMemberRepository repository,
    ILogger<MoveMemberInHierarchyHandler> logger) : IRequestHandler<MoveMemberInHierarchyCommand, Result>
{
    public async Task<Result> Handle(MoveMemberInHierarchyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the member
            var member = await repository.GetMemberAsync(request.MemberId, Guid.Empty, cancellationToken);
            if (member == null)
            {
                return Result.Failure($"Member with ID {request.MemberId} not found");
            }

            // If moving to root (no parent)
            if (request.NewParentId == null)
            {
                member.ParentMemberId = null;
                await repository.UpdateHierarchyPathAsync(member.Id, member.Id.ToString(), cancellationToken);

                logger.LogInformation("Moved member {MemberId} to root level", request.MemberId);
                return Result.Success();
            }

            // Get the new parent member
            var newParent = await repository.GetMemberAsync(request.NewParentId.Value, Guid.Empty, cancellationToken);
            if (newParent == null)
            {
                return Result.Failure($"New parent member with ID {request.NewParentId} not found");
            }

            // Validate same tenant
            if (member.TenantId != newParent.TenantId)
            {
                return Result.Failure("New parent member must be in the same tenant");
            }

            // Prevent circular reference
            if (newParent.HierarchyPath != null &&
                newParent.HierarchyPath.Contains(member.Id.ToString()))
            {
                return Result.Failure("Cannot move member: would create circular reference");
            }

            // Update member hierarchy
            member.ParentMemberId = request.NewParentId;
            var newHierarchyPath = newParent.HierarchyPath != null
                ? $"{newParent.HierarchyPath}/{member.Id}"
                : member.Id.ToString();

            await repository.UpdateHierarchyPathAsync(member.Id, newHierarchyPath, cancellationToken);

            logger.LogInformation("Moved member {MemberId} to parent {ParentId}",
                request.MemberId, request.NewParentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error moving member {MemberId} to parent {ParentId}",
                request.MemberId, request.NewParentId);
            return Result.Failure($"Failed to move member in hierarchy: {ex.Message}");
        }
    }
}
