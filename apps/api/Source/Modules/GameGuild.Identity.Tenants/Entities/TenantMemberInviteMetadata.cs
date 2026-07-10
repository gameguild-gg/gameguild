using System.Text.Json;

namespace GameGuild.Identity.Tenants;

public static class TenantMemberInviteStatuses
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Cancelled = "Cancelled";
}

public sealed record TenantMemberInviteMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string? InviteStatus { get; init; }

    public string? InvitedByEmail { get; init; }

    public string? InviteeEmail { get; init; }

    public string? InviteeName { get; init; }

    public DateTime? InvitedAt { get; init; }

    public DateTime? LastSentAt { get; init; }

    public DateTime? AcceptedAt { get; init; }

    public DateTime? CancelledAt { get; init; }

    public int ResendCount { get; init; }

    public static TenantMemberInviteMetadata Empty { get; } = new();

    public static TenantMemberInviteMetadata CreatePending(string? invitedByEmail, DateTime now, string? inviteeEmail = null, string? inviteeName = null)
    {
        return new TenantMemberInviteMetadata
        {
            InviteStatus = TenantMemberInviteStatuses.Pending,
            InvitedByEmail = string.IsNullOrWhiteSpace(invitedByEmail) ? null : invitedByEmail.Trim(),
            InviteeEmail = NormalizeEmail(inviteeEmail),
            InviteeName = string.IsNullOrWhiteSpace(inviteeName) ? null : inviteeName.Trim(),
            InvitedAt = now,
            LastSentAt = now,
            ResendCount = 1
        };
    }

    public static TenantMemberInviteMetadata FromJson(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<TenantMemberInviteMetadata>(metadata, JsonOptions) ?? Empty;
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public TenantMemberInviteMetadata MarkResent(string? actorEmail, DateTime now)
    {
        return this with
        {
            InviteStatus = TenantMemberInviteStatuses.Pending,
            InvitedByEmail = string.IsNullOrWhiteSpace(InvitedByEmail) ? NormalizeEmail(actorEmail) : InvitedByEmail,
            LastSentAt = now,
            ResendCount = Math.Max(ResendCount, 0) + 1,
            CancelledAt = null
        };
    }

    public TenantMemberInviteMetadata MarkAccepted(DateTime now)
    {
        return this with
        {
            InviteStatus = TenantMemberInviteStatuses.Accepted,
            AcceptedAt = now,
            CancelledAt = null
        };
    }

    public TenantMemberInviteMetadata MarkCancelled(DateTime now)
    {
        return this with
        {
            InviteStatus = TenantMemberInviteStatuses.Cancelled,
            CancelledAt = now
        };
    }

    private static string? NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
