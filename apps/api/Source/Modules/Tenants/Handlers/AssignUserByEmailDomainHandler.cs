using GameGuild.Core;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for auto-assigning users to tenants based on email domain
/// </summary>
public sealed class AssignUserByEmailDomainHandler(
    ITenantDomainsService tenantDomainsService,
    ITenantMemberRepository memberRepository,
    ITenantRepository tenantRepository,
    ILogger<AssignUserByEmailDomainHandler> logger) : IRequestHandler<AssignUserByEmailDomainCommand, Result<TenantMemberDto>>
{
    public async Task<Result<TenantMemberDto>> Handle(AssignUserByEmailDomainCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Extract domain from email
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            {
                return Result<TenantMemberDto>.Failure("Invalid email format");
            }

            var emailParts = request.Email.Split('@');
            if (emailParts.Length != 2)
            {
                return Result<TenantMemberDto>.Failure("Invalid email format");
            }

            var domain = emailParts[1];

            // Check for subdomain
            string topLevelDomain;
            string? subdomain = null;

            var domainParts = domain.Split('.');
            if (domainParts.Length >= 2)
            {
                // Extract TLD and subdomain if exists
                topLevelDomain = string.Join(".", domainParts.Skip(domainParts.Length - 2));
                if (domainParts.Length > 2)
                {
                    subdomain = string.Join(".", domainParts.Take(domainParts.Length - 2));
                }
            }
            else
            {
                topLevelDomain = domain;
            }

            logger.LogInformation("Attempting to find tenant for domain {Domain} (TLD: {TLD}, Subdomain: {Subdomain})",
                domain, topLevelDomain, subdomain);

            // Find tenant by domain
            var tenant = await tenantDomainsService.FindTenantByDomainAsync(topLevelDomain, subdomain, cancellationToken);

            if (tenant == null)
            {
                return Result<TenantMemberDto>.Failure($"No tenant found for email domain {domain}");
            }

            // Check if already a member
            var existingMember = await memberRepository.GetMemberAsync(request.UserId, tenant.Id, cancellationToken);
            if (existingMember != null)
            {
                logger.LogInformation("User {UserId} is already a member of tenant {TenantId}",
                    request.UserId, tenant.Id);

                return Result<TenantMemberDto>.Success(new TenantMemberDto
                {
                    UserId = existingMember.UserId,
                    TenantId = existingMember.TenantId,
                    Role = existingMember.Role,
                    IsActive = existingMember.IsActive,
                    JoinedAt = existingMember.JoinedAt,
                    LeftAt = existingMember.LeftAt,
                    LeaveReason = existingMember.LeaveReason,
                    TenantName = tenant.Name,
                    TenantSlug = tenant.Slug
                });
            }

            // Create new membership
            var member = new TenantMember
            {
                UserId = request.UserId,
                TenantId = tenant.Id,
                Role = request.DefaultRole,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var addedMember = await memberRepository.AddMemberAsync(member, cancellationToken);

            logger.LogInformation("Auto-assigned user {UserId} to tenant {TenantId} with role {Role} based on email domain {Domain}",
                request.UserId, tenant.Id, request.DefaultRole, domain);

            var dto = new TenantMemberDto
            {
                UserId = addedMember.UserId,
                TenantId = addedMember.TenantId,
                Role = addedMember.Role,
                IsActive = addedMember.IsActive,
                JoinedAt = addedMember.JoinedAt,
                LeftAt = addedMember.LeftAt,
                LeaveReason = addedMember.LeaveReason,
                TenantName = tenant.Name,
                TenantSlug = tenant.Slug
            };

            return Result<TenantMemberDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auto-assigning user {UserId} with email {Email}",
                request.UserId, request.Email);
            return Result<TenantMemberDto>.Failure($"Failed to auto-assign user: {ex.Message}");
        }
    }
}
