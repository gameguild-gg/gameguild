using System.Net;
using GameGuild.CQRS;
using GameGuild.Email;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Identity.Tenants;

public sealed class UpdateTenantMemberInviteCommandHandler(
    ITenantMemberRepository memberRepository,
    IEmailSender? emailSender = null,
    IConfiguration? configuration = null)
    : ICommandHandler<UpdateTenantMemberInviteCommand, UpdateTenantMemberInviteResponse>
{
    public async Task<UpdateTenantMemberInviteResponse> Handle(UpdateTenantMemberInviteCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository
            .GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member == null)
        {
            return new UpdateTenantMemberInviteResponse
            {
                Success = false,
                Message = "Membership invite not found"
            };
        }

        if (request.Action == TenantMemberInviteAction.Cancel && member.Tenant?.IsDefault == true)
        {
            return new UpdateTenantMemberInviteResponse
            {
                Success = false,
                Message = "The default tenant membership cannot be cancelled.",
                MemberId = member.Id
            };
        }

        var metadata = TenantMemberInviteMetadata.FromJson(member.Metadata);
        if (!IsPendingInvite(member, metadata))
        {
            return new UpdateTenantMemberInviteResponse
            {
                Success = false,
                Message = "Only pending membership invites can be updated",
                MemberId = member.Id,
                InviteStatus = metadata.InviteStatus
            };
        }

        var now = SystemClock.UtcNow;
        metadata = request.Action switch
        {
            TenantMemberInviteAction.Resend => metadata.MarkResent(request.ActorEmail, now),
            TenantMemberInviteAction.Cancel => CancelInvite(member, metadata, now),
            TenantMemberInviteAction.Accept => AcceptInvite(member, metadata, now),
            _ => metadata
        };

        member.Metadata = metadata.ToJson();
        await memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);

        if (request.Action == TenantMemberInviteAction.Resend)
        {
            await SendInviteEmailAsync(member, metadata, request.ActorEmail, cancellationToken).ConfigureAwait(false);
        }

        return new UpdateTenantMemberInviteResponse
        {
            Success = true,
            Message = request.Action switch
            {
                TenantMemberInviteAction.Resend => "Invite resent",
                TenantMemberInviteAction.Cancel => "Invite cancelled",
                TenantMemberInviteAction.Accept => "Invite accepted",
                _ => "Invite updated"
            },
            MemberId = member.Id,
            InviteStatus = metadata.InviteStatus
        };
    }

    private static bool IsPendingInvite(TenantMember member, TenantMemberInviteMetadata metadata)
    {
        return !member.IsActive && string.Equals(metadata.InviteStatus, TenantMemberInviteStatuses.Pending, StringComparison.OrdinalIgnoreCase);
    }

    private static TenantMemberInviteMetadata CancelInvite(TenantMember member, TenantMemberInviteMetadata metadata, DateTime now)
    {
        member.Deactivate("Invite cancelled");
        return metadata.MarkCancelled(now);
    }

    private static TenantMemberInviteMetadata AcceptInvite(TenantMember member, TenantMemberInviteMetadata metadata, DateTime now)
    {
        member.Activate();
        member.JoinedAt = now;
        return metadata.MarkAccepted(now);
    }

    private async Task SendInviteEmailAsync(
        TenantMember member,
        TenantMemberInviteMetadata metadata,
        string? actorEmail,
        CancellationToken cancellationToken)
    {
        if (emailSender == null || string.IsNullOrWhiteSpace(metadata.InviteeEmail))
        {
            return;
        }

        var tenantName = string.IsNullOrWhiteSpace(member.Tenant?.Name) ? "GameGuild" : member.Tenant!.Name;
        var recipientName = string.IsNullOrWhiteSpace(metadata.InviteeName) ? metadata.InviteeEmail.Trim() : metadata.InviteeName.Trim();
        var inviter = string.IsNullOrWhiteSpace(actorEmail)
            ? metadata.InvitedByEmail ?? "A GameGuild administrator"
            : actorEmail.Trim();
        var reviewUrl = BuildReviewUrl();
        var activationUrl = BuildActivationUrl(metadata.InviteeEmail);
        var plainTextContent =
            $"Hi {recipientName},\n\n{inviter} resent your invitation to join {tenantName} on GameGuild as {member.Role}.\n\nReview and accept your access:\n{reviewUrl}\n\nIf this is your first GameGuild invitation, set your password first:\n{activationUrl}\n\nIf you were not expecting this invite, you can ignore this email.";
        var htmlContent =
            $"<p>Hi {WebUtility.HtmlEncode(recipientName)},</p><p>{WebUtility.HtmlEncode(inviter)} resent your invitation to join <strong>{WebUtility.HtmlEncode(tenantName)}</strong> on GameGuild as <strong>{WebUtility.HtmlEncode(member.Role)}</strong>.</p><p><a href=\"{WebUtility.HtmlEncode(reviewUrl)}\">Review and accept your access</a></p><p>First time on GameGuild? <a href=\"{WebUtility.HtmlEncode(activationUrl)}\">Set your password</a>, then return to your invitations.</p><p>If you were not expecting this invite, you can ignore this email.</p>";

        await emailSender.SendAsync(
            new EmailMessage(
                metadata.InviteeEmail.Trim(),
                $"Reminder: you were invited to {tenantName} on GameGuild",
                plainTextContent,
                htmlContent,
                recipientName),
            cancellationToken).ConfigureAwait(false);
    }

    private string BuildReviewUrl()
    {
        var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
        var callbackPath = "/dashboard/invitations";
        return $"{appBaseUrl.TrimEnd('/')}/sign-in?callbackUrl={Uri.EscapeDataString(callbackPath)}";
    }

    private string BuildActivationUrl(string email)
    {
        var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
        return $"{appBaseUrl.TrimEnd('/')}/forgot-password?email={Uri.EscapeDataString(email.Trim())}";
    }
}
