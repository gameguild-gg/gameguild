using GameGuild.Compliance.Consent;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;

namespace GameGuild.API.Core.Compliance;

public sealed record RecordUserConsentEndpointCommand(bool AnalyticsGranted, bool MarketingGranted, string? Method) : ICommand<RecordUserConsentEndpointCommand.Result>
{
    public sealed record Result(int ChangedRecords);
}

/// <summary>
///     Grants or revokes the seeded analytics/marketing UserConsent records
///     when (and only when) the effective decision differs from the persisted
///     record, so repeated banner views never spam audit rows.
/// </summary>
public sealed class RecordUserConsentEndpointCommandHandler(
    IConsentService consentService,
    IConsentPolicyRepository policyRepository,
    IPolicyVersionRepository versionRepository,
    IUserConsentRepository userConsentRepository,
    IActorContextAccessor actorContextAccessor,
    IHttpContextAccessor httpContextAccessor) : ICommandHandler<RecordUserConsentEndpointCommand, RecordUserConsentEndpointCommand.Result>
{
    private static readonly string[] AllowedMethods = ["banner", "settings"];

    public async Task<RecordUserConsentEndpointCommand.Result> Handle(RecordUserConsentEndpointCommand request, CancellationToken ct)
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid
            ?? throw new UnauthorizedAccessException("Consent recording requires an authenticated actor with a resolvable subject identifier.");

        var method = NormalizeMethod(request.Method);
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua[..Math.Min(ua.Length, 500)] : null;

        var changed = 0;
        changed += await SyncPurposeAsync(userId, UserConsentPolicyNames.Analytics, request.AnalyticsGranted, method, ipAddress, userAgent, ct).ConfigureAwait(false);
        changed += await SyncPurposeAsync(userId, UserConsentPolicyNames.Marketing, request.MarketingGranted, method, ipAddress, userAgent, ct).ConfigureAwait(false);
        return new RecordUserConsentEndpointCommand.Result(changed);
    }

    private async Task<int> SyncPurposeAsync(
        Guid userId, string policyName, bool granted, string method, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var policies = await policyRepository.GetAllActiveAsync(tenantId: null, ct).ConfigureAwait(false);
        var policy = policies.FirstOrDefault(p => string.Equals(p.Name, policyName, StringComparison.OrdinalIgnoreCase));
        if (policy is null)
        {
            return 0;
        }

        var version = await versionRepository.GetCurrentVersionAsync(policy.Id, ct).ConfigureAwait(false);
        if (version is null)
        {
            return 0;
        }

        var existing = await userConsentRepository.GetAsync(userId, version.Id, ct).ConfigureAwait(false);
        if (existing?.IsGranted == granted)
        {
            return 0;
        }

        if (granted)
        {
            await consentService.GrantConsentAsync(userId, version.Id, ipAddress, userAgent, method, ct).ConfigureAwait(false);
        }
        else
        {
            await consentService.RevokeConsentAsync(userId, version.Id, ct).ConfigureAwait(false);
        }

        return 1;
    }

    private static string NormalizeMethod(string? method)
    {
        if (!string.IsNullOrEmpty(method))
        {
            var match = AllowedMethods.FirstOrDefault(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }
        return "banner";
    }
}

public static class UserConsentPolicyNames
{
    public const string Analytics = "Analytics Consent";
    public const string Marketing = "Marketing Consent";
}
