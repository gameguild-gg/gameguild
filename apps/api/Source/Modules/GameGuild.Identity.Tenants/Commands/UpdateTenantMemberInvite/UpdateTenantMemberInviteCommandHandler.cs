using GameGuild.CQRS;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Identity.Tenants;

public sealed class UpdateTenantMemberInviteCommandHandler(
    ITenantMemberRepository memberRepository,
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
            QueueInviteEmail(member, metadata, request.ActorEmail);
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

    private void QueueInviteEmail(TenantMember member, TenantMemberInviteMetadata metadata, string? actorEmail)
    {
        if (string.IsNullOrWhiteSpace(metadata.InviteeEmail))
        {
            return;
        }

        member.AddDomainEvent(new TenantInviteRequestedNotification(
            member.TenantId,
            metadata.InviteeEmail.Trim(),
            metadata.InviteeName,
            string.IsNullOrWhiteSpace(actorEmail) ? metadata.InvitedByEmail : actorEmail.Trim(),
            member.Tenant?.Name ?? "GameGuild",
            member.Role,
            BuildReviewUrl(),
            BuildActivationUrl(metadata.InviteeEmail),
            resend: true));
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
