using GameGuild.Core;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for getting direct children of a member
/// </summary>
public sealed class GetMemberChildrenHandler(
    ITenantMemberRepository repository,
    ITenantRepository tenantRepository,
    ILogger<GetMemberChildrenHandler> logger) : IRequestHandler<GetMemberChildrenQuery, Result<IReadOnlyList<TenantMemberDto>>>
{
    public async Task<Result<IReadOnlyList<TenantMemberDto>>> Handle(GetMemberChildrenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var children = await repository.GetChildMembersAsync(request.MemberId, cancellationToken);

            var dtos = new List<TenantMemberDto>();
            foreach (var child in children)
            {
                var tenant = await tenantRepository.GetByIdAsync(child.TenantId, cancellationToken);
                dtos.Add(new TenantMemberDto
                {
                    UserId = child.UserId,
                    TenantId = child.TenantId,
                    Role = child.Role,
                    IsActive = child.IsActive,
                    JoinedAt = child.JoinedAt,
                    LeftAt = child.LeftAt,
                    LeaveReason = child.LeaveReason,
                    TenantName = tenant?.Name,
                    TenantSlug = tenant?.Slug
                });
            }

            logger.LogInformation("Retrieved {Count} children for member {MemberId}", dtos.Count, request.MemberId);

            return Result<IReadOnlyList<TenantMemberDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving children for member {MemberId}", request.MemberId);
            return Result<IReadOnlyList<TenantMemberDto>>.Failure($"Failed to retrieve children: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for getting complete hierarchy for a member
/// </summary>
public sealed class GetMemberHierarchyHandler(
    ITenantMemberRepository repository,
    ITenantRepository tenantRepository,
    ILogger<GetMemberHierarchyHandler> logger) : IRequestHandler<GetMemberHierarchyQuery, Result<IReadOnlyList<TenantMemberDto>>>
{
    public async Task<Result<IReadOnlyList<TenantMemberDto>>> Handle(GetMemberHierarchyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var hierarchy = await repository.GetMemberHierarchyAsync(request.MemberId, cancellationToken);

            var dtos = new List<TenantMemberDto>();
            foreach (var member in hierarchy)
            {
                var tenant = await tenantRepository.GetByIdAsync(member.TenantId, cancellationToken);
                dtos.Add(new TenantMemberDto
                {
                    UserId = member.UserId,
                    TenantId = member.TenantId,
                    Role = member.Role,
                    IsActive = member.IsActive,
                    JoinedAt = member.JoinedAt,
                    LeftAt = member.LeftAt,
                    LeaveReason = member.LeaveReason,
                    TenantName = tenant?.Name,
                    TenantSlug = tenant?.Slug
                });
            }

            logger.LogInformation("Retrieved {Count} members in hierarchy for member {MemberId}", dtos.Count, request.MemberId);

            return Result<IReadOnlyList<TenantMemberDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving hierarchy for member {MemberId}", request.MemberId);
            return Result<IReadOnlyList<TenantMemberDto>>.Failure($"Failed to retrieve hierarchy: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for getting the entire tenant hierarchy tree
/// </summary>
public sealed class GetTenantHierarchyTreeHandler(
    ITenantMemberRepository repository,
    ITenantRepository tenantRepository,
    ILogger<GetTenantHierarchyTreeHandler> logger) : IRequestHandler<GetTenantHierarchyTreeQuery, Result<IReadOnlyList<TenantMemberDto>>>
{
    public async Task<Result<IReadOnlyList<TenantMemberDto>>> Handle(GetTenantHierarchyTreeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var rootMembers = await repository.GetRootMembersAsync(request.TenantId, cancellationToken);

            var dtos = new List<TenantMemberDto>();
            foreach (var member in rootMembers)
            {
                var tenant = await tenantRepository.GetByIdAsync(member.TenantId, cancellationToken);
                dtos.Add(new TenantMemberDto
                {
                    UserId = member.UserId,
                    TenantId = member.TenantId,
                    Role = member.Role,
                    IsActive = member.IsActive,
                    JoinedAt = member.JoinedAt,
                    LeftAt = member.LeftAt,
                    LeaveReason = member.LeaveReason,
                    TenantName = tenant?.Name,
                    TenantSlug = tenant?.Slug
                });
            }

            logger.LogInformation("Retrieved {Count} root members for tenant {TenantId}", dtos.Count, request.TenantId);

            return Result<IReadOnlyList<TenantMemberDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving hierarchy tree for tenant {TenantId}", request.TenantId);
            return Result<IReadOnlyList<TenantMemberDto>>.Failure($"Failed to retrieve hierarchy tree: {ex.Message}");
        }
    }
}
