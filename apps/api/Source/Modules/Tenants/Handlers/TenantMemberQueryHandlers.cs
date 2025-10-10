using GameGuild.Core;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for getting tenant members
/// </summary>
public sealed class GetTenantMembersHandler(
    ITenantMemberRepository repository,
    ILogger<GetTenantMembersHandler> logger) : IRequestHandler<GetTenantMembersQuery, Result<IReadOnlyList<TenantMemberDto>>>
{
    public async Task<Result<IReadOnlyList<TenantMemberDto>>> Handle(GetTenantMembersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var members = request.ActiveOnly
                ? await repository.GetActiveMembersAsync(request.TenantId, cancellationToken)
                : await repository.GetMembersByTenantIdAsync(request.TenantId, cancellationToken);

            var dtos = members.Select(m => new TenantMemberDto
            {
                UserId = m.UserId,
                TenantId = m.TenantId,
                Role = m.Role,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt,
                LeftAt = m.LeftAt,
                LeaveReason = m.LeaveReason
            }).ToList();

            return Result<IReadOnlyList<TenantMemberDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tenant members");
            return Result<IReadOnlyList<TenantMemberDto>>.Failure($"Error getting members: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for getting user's tenants
/// </summary>
public sealed class GetUserTenantsHandler(
    ITenantMemberRepository repository,
    ILogger<GetUserTenantsHandler> logger) : IRequestHandler<GetUserTenantsQuery, Result<IReadOnlyList<TenantMemberDto>>>
{
    public async Task<Result<IReadOnlyList<TenantMemberDto>>> Handle(GetUserTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var memberships = await repository.GetTenantsByUserIdAsync(request.UserId, cancellationToken);

            if (request.ActiveOnly)
            {
                memberships = memberships.Where(m => m.IsActive).ToList();
            }

            var dtos = memberships.Select(m => new TenantMemberDto
            {
                UserId = m.UserId,
                TenantId = m.TenantId,
                Role = m.Role,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt,
                LeftAt = m.LeftAt,
                LeaveReason = m.LeaveReason,
                TenantName = m.Tenant?.Name,
                TenantSlug = m.Tenant?.Slug
            }).ToList();

            return Result<IReadOnlyList<TenantMemberDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user tenants");
            return Result<IReadOnlyList<TenantMemberDto>>.Failure($"Error getting tenants: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for getting a specific tenant member
/// </summary>
public sealed class GetTenantMemberHandler(
    ITenantMemberRepository repository,
    ILogger<GetTenantMemberHandler> logger) : IRequestHandler<GetTenantMemberQuery, Result<TenantMemberDto>>
{
    public async Task<Result<TenantMemberDto>> Handle(GetTenantMemberQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var member = await repository.GetMemberAsync(request.UserId, request.TenantId, cancellationToken);
            if (member == null)
            {
                return Result<TenantMemberDto>.Failure("Member not found");
            }

            var dto = new TenantMemberDto
            {
                UserId = member.UserId,
                TenantId = member.TenantId,
                Role = member.Role,
                IsActive = member.IsActive,
                JoinedAt = member.JoinedAt,
                LeftAt = member.LeftAt,
                LeaveReason = member.LeaveReason
            };

            return Result<TenantMemberDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tenant member");
            return Result<TenantMemberDto>.Failure($"Error getting member: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for checking if user is member of tenant
/// </summary>
public sealed class IsMemberOfTenantHandler(
    ITenantMemberRepository repository,
    ILogger<IsMemberOfTenantHandler> logger) : IRequestHandler<IsMemberOfTenantQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(IsMemberOfTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var isMember = await repository.IsMemberOfTenantAsync(request.UserId, request.TenantId, cancellationToken);
            return Result<bool>.Success(isMember);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking tenant membership");
            return Result<bool>.Failure($"Error checking membership: {ex.Message}");
        }
    }
}
