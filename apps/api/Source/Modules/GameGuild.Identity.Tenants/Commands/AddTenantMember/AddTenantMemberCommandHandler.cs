using GameGuild.CQRS;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for adding a tenant member
/// </summary>
public sealed class AddTenantMemberCommandHandler(
    ITenantRepository tenantRepository,
    ITenantMemberRepository memberRepository,
    IConfiguration? configuration = null) : ICommandHandler<AddTenantMemberCommand, AddTenantMemberResponse>
{
    public async Task<AddTenantMemberResponse> Handle(AddTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new AddTenantMemberResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found" }; }

        var existingMember = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsDefault && existingMember == null)
        {
            existingMember = await memberRepository
                .GetByUserAndTenantIncludingDeletedAsync(request.UserId, request.TenantId, cancellationToken)
                .ConfigureAwait(false);
        }

        var now = SystemClock.UtcNow;
        var requiresAcceptance = request.RequiresAcceptance && !tenant.IsDefault;
        if (existingMember != null)
        {
            if (existingMember.IsActive && existingMember.DeletedAt is null && !tenant.IsDefault)
            {
                return new AddTenantMemberResponse { Success = false, Message = "User is already a member of this tenant" };
            }

            if (existingMember.DeletedAt is not null)
            {
                existingMember.Restore();
            }

            if (!existingMember.IsActive)
            {
                if (!tenant.IsDefault || string.IsNullOrWhiteSpace(existingMember.Role))
                {
                    existingMember.UpdateRole(request.Role);
                }
                existingMember.JoinedAt = now;
            }

            if (requiresAcceptance)
            {
                existingMember.IsActive = false;
                existingMember.LeftAt = null;
                existingMember.LeaveReason = null;
                existingMember.Metadata = TenantMemberInviteMetadata.CreatePending(
                    request.InvitedByEmail,
                    now,
                    request.InviteeEmail,
                    request.InviteeName).ToJson();
            }
            else
            {
                existingMember.Activate();
                var invite = string.IsNullOrWhiteSpace(existingMember.Metadata) && request.RequiresAcceptance
                    ? TenantMemberInviteMetadata.CreatePending(request.InvitedByEmail, now, request.InviteeEmail, request.InviteeName)
                    : TenantMemberInviteMetadata.FromJson(existingMember.Metadata);
                if (!string.IsNullOrWhiteSpace(invite.InviteStatus))
                {
                    existingMember.Metadata = invite.MarkAccepted(now).ToJson();
                }
            }

            await memberRepository.UpdateAsync(existingMember, cancellationToken).ConfigureAwait(false);

            if (request.RequiresAcceptance)
            {
                QueueInviteEmail(tenant, request);
            }

            tenant.AddDomainEvent(new TenantMemberAddedEvent(request.TenantId, request.UserId, request.InvitedByEmail ?? "unknown@email.com", request.Role));

            return new AddTenantMemberResponse
            {
                Success = true,
                Message = requiresAcceptance ? "Membership invite recreated" : "Member reactivated successfully",
                MemberId = existingMember.Id
            };
        }

        var member = new TenantMember { TenantId = request.TenantId, UserId = request.UserId, Role = request.Role, JoinedAt = now, IsActive = true };

        if (requiresAcceptance)
        {
            member.IsActive = false;
            member.Metadata = TenantMemberInviteMetadata.CreatePending(request.InvitedByEmail, now, request.InviteeEmail, request.InviteeName).ToJson();
        }
        else if (request.RequiresAcceptance)
        {
            member.Metadata = TenantMemberInviteMetadata
                .CreatePending(request.InvitedByEmail, now, request.InviteeEmail, request.InviteeName)
                .MarkAccepted(now)
                .ToJson();
        }

        var createdMember = await memberRepository.CreateAsync(member, cancellationToken).ConfigureAwait(false);

        if (request.RequiresAcceptance)
        {
            QueueInviteEmail(tenant, request);
        }

        tenant.AddDomainEvent(new TenantMemberAddedEvent(request.TenantId, request.UserId, request.InvitedByEmail ?? "unknown@email.com", request.Role));

        return new AddTenantMemberResponse { Success = true, Message = "Member added successfully", MemberId = createdMember.Id };
    }

    private void QueueInviteEmail(Tenant tenant, AddTenantMemberCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.InviteeEmail))
        {
            return;
        }

        tenant.AddDomainEvent(new TenantInviteRequestedNotification(
            request.TenantId,
            request.InviteeEmail.Trim(),
            request.InviteeName,
            request.InvitedByEmail,
            tenant.Name,
            request.Role,
            BuildReviewUrl(),
            BuildActivationUrl(request.InviteeEmail),
            resend: false));
    }

    private string BuildReviewUrl()
    {
        var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
        var configuredPath = configuration?["Identity:Invitations:ReviewPath"];
        var callbackPath = string.IsNullOrWhiteSpace(configuredPath)
            ? "/invitations"
            : $"/{configuredPath.Trim().TrimStart('/')}";
        return $"{appBaseUrl.TrimEnd('/')}/sign-in?callbackUrl={Uri.EscapeDataString(callbackPath)}";
    }

    private string BuildActivationUrl(string email)
    {
        var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
        return $"{appBaseUrl.TrimEnd('/')}/forgot-password?email={Uri.EscapeDataString(email.Trim())}";
    }
}
