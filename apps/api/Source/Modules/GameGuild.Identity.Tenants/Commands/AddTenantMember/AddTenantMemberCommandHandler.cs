using System.Net;
using GameGuild.CQRS;
using GameGuild.Email;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for adding a tenant member
/// </summary>
public sealed class AddTenantMemberCommandHandler(
    ITenantRepository tenantRepository,
    ITenantMemberRepository memberRepository,
    IEmailSender? emailSender = null,
    IConfiguration? configuration = null) : ICommandHandler<AddTenantMemberCommand, AddTenantMemberResponse>
{
    public async Task<AddTenantMemberResponse> Handle(AddTenantMemberCommand request, CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new AddTenantMemberResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found" }; }

        // Check if member already exists
        var existingMember = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);

        if (existingMember != null) { return new AddTenantMemberResponse { Success = false, Message = "User is already a member of this tenant" }; }

        var now = SystemClock.UtcNow;
        var member = new TenantMember { TenantId = request.TenantId, UserId = request.UserId, Role = request.Role, JoinedAt = now, IsActive = true };

        if (request.RequiresAcceptance)
        {
            member.IsActive = false;
            member.Metadata = TenantMemberInviteMetadata.CreatePending(request.InvitedByEmail, now, request.InviteeEmail, request.InviteeName).ToJson();
        }

        var createdMember = await memberRepository.CreateAsync(member, cancellationToken).ConfigureAwait(false);

        if (request.RequiresAcceptance)
        {
            await SendInviteEmailAsync(tenant, request, cancellationToken).ConfigureAwait(false);
        }

        // Raise domain event
        tenant.AddDomainEvent(new TenantMemberAddedEvent(request.TenantId, request.UserId, request.InvitedByEmail ?? "unknown@email.com", request.Role));

        return new AddTenantMemberResponse { Success = true, Message = "Member added successfully", MemberId = createdMember.Id };
    }

    private async Task SendInviteEmailAsync(Tenant tenant, AddTenantMemberCommand request, CancellationToken cancellationToken)
    {
        if (emailSender == null || string.IsNullOrWhiteSpace(request.InviteeEmail))
        {
            return;
        }

        var recipientName = string.IsNullOrWhiteSpace(request.InviteeName)
            ? request.InviteeEmail.Trim()
            : request.InviteeName.Trim();
        var signInUrl = BuildSignInUrl();
        var inviter = string.IsNullOrWhiteSpace(request.InvitedByEmail) ? "A GameGuild administrator" : request.InvitedByEmail.Trim();
        var plainTextContent =
            $"Hi {recipientName},\n\n{inviter} invited you to join {tenant.Name} on GameGuild as {request.Role}.\n\nSign in to review and accept your access:\n{signInUrl}\n\nIf you were not expecting this invite, you can ignore this email.";
        var htmlContent =
            $"<p>Hi {WebUtility.HtmlEncode(recipientName)},</p><p>{WebUtility.HtmlEncode(inviter)} invited you to join <strong>{WebUtility.HtmlEncode(tenant.Name)}</strong> on GameGuild as <strong>{WebUtility.HtmlEncode(request.Role)}</strong>.</p><p><a href=\"{WebUtility.HtmlEncode(signInUrl)}\">Sign in to review and accept your access</a></p><p>If you were not expecting this invite, you can ignore this email.</p>";

        await emailSender.SendAsync(
            new EmailMessage(
                request.InviteeEmail.Trim(),
                $"You were invited to {tenant.Name} on GameGuild",
                plainTextContent,
                htmlContent,
                recipientName),
            cancellationToken).ConfigureAwait(false);
    }

    private string BuildSignInUrl()
    {
        var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
        var callbackPath = "/dashboard/community/members/users";
        return $"{appBaseUrl.TrimEnd('/')}/sign-in?callbackUrl={Uri.EscapeDataString(callbackPath)}";
    }
}
