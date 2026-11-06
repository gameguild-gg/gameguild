using GameGuild.Compliance.Consent;

namespace GameGuild.API.Core.Compliance;

/// <summary>
///     Seeds the consent policies backing the web client's granular consent
///     surfaces (issue #1206): "Analytics Consent" and "Marketing Consent".
/// </summary>
/// <remarks>
///     Host-side on purpose: the Compliance.Consent module is platform-generic
///     and must stay free of product policy names (module purity per AGENTS.md).
///     Idempotent — safe on every boot; existing policies and their published
///     versions are left untouched.
/// </remarks>
public static class ConsentPolicySeeder
{
    private sealed record PolicySpec(string Name, PolicyType Type, string Description);

    private static readonly PolicySpec[] Specs =
    [
        new(UserConsentPolicyNames.Analytics, PolicyType.Custom,
            "Granular consent to analytics cookies and measurement (GA4/GTM site-usage tracking)."),
        new(UserConsentPolicyNames.Marketing, PolicyType.MarketingConsent,
            "Granular consent to marketing/advertising cookies and campaign measurement."),
    ];

    public static async Task SeedAsync(
        IConsentService consentService,
        IConsentPolicyRepository policyRepository,
        IPolicyVersionRepository versionRepository,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        foreach (var spec in Specs)
        {
            var policies = await policyRepository.GetAllActiveAsync(tenantId: null, ct).ConfigureAwait(false);
            var policy = policies.FirstOrDefault(p => string.Equals(p.Name, spec.Name, StringComparison.OrdinalIgnoreCase));

            Guid policyId;
            if (policy is null)
            {
                var created = await consentService.CreatePolicyAsync(spec.Name, spec.Type, isMandatory: false, spec.Description, ct).ConfigureAwait(false);
                policyId = created.Id;
                logger?.LogInformation("Seeded consent policy {PolicyName} ({PolicyId})", spec.Name, policyId);
            }
            else
            {
                policyId = policy.Id;
            }

            var currentVersion = await versionRepository.GetCurrentVersionAsync(policyId, ct).ConfigureAwait(false);
            if (currentVersion is null)
            {
                await consentService.PublishVersionAsync(policyId, "1.0", spec.Description ?? spec.Name, ContentType.PlainText, ct).ConfigureAwait(false);
                logger?.LogInformation("Published consent policy version 1.0 for {PolicyName}", spec.Name);
            }
        }
    }
}
