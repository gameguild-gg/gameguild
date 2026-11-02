using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Audit;

/// <summary>A template entry of the platform processing-activity catalog used to seed the Article 30 register.</summary>
public sealed record GdprProcessingActivityTemplate(
    string ActivityName,
    string Purpose,
    string LegalBasis,
    string CategoriesOfDataSubjects,
    string CategoriesOfPersonalData,
    string Recipients,
    bool InvolvesCrossBorderTransfers,
    string? TransferDestinations,
    string RetentionPeriod,
    string SecurityMeasures);

public interface IGdprArticle30Service
{
    Task<IReadOnlyList<GdprProcessingActivity>> GetActivitiesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<GdprProcessingActivity?> GetActivityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GdprProcessingActivity> SaveActivityAsync(GdprProcessingActivity activity, CancellationToken cancellationToken = default);
    Task<bool> DeleteActivityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GdprProcessingActivity?> MarkReviewedAsync(Guid id, Guid reviewerUserId, CancellationToken cancellationToken = default);
    /// <summary>(Re)derives the auto-generated platform processing activities for a tenant; manual entries are never overwritten.</summary>
    Task<int> SeedPlatformActivitiesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    /// <summary>Reports Article 30 completeness: activities missing required attributes are listed so gaps are visible.</summary>
    Task<IReadOnlyList<string>> GetCompletenessGapsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// GDPR Article 30 processing activity register (issue #186): maintains the record of processing
/// activities — purposes, categories of data subjects and personal data, recipients, transfers,
/// retention and security measures — with a seeded catalog derived from the platform's own
/// processing activities and manual tenant-specific entries.
/// </summary>
public sealed class GdprArticle30Service(
    IApplicationDbContext context,
    TimeProvider timeProvider) : IGdprArticle30Service
{
    /// <summary>Processing activities the platform itself performs; auto entries are regenerated from this catalog.</summary>
    internal static readonly IReadOnlyList<GdprProcessingActivityTemplate> PlatformCatalog =
    [
        new("Identity and authentication", "User authentication, session management and multi-factor verification", "contract performance",
            "Platform users", "email address, name, authentication metadata, IP address, device identifiers", "none (internal processing)",
            false, null, "Active sessions 30 days; authentication audit 400 days", "TLS in transit, at-rest encryption, MFA, tamper-evident audit"),
        new("User account management", "Provisioning, profile maintenance, suspension and deletion of user accounts", "contract performance",
            "Platform users", "name, email address, profile attributes, tenant membership", "tenant administrators",
            false, null, "Account lifetime plus 90 days", "role-based access control, field masking, permission audit log"),
        new("Tenant administration", "Tenant provisioning, configuration and membership administration", "legitimate interest",
            "Tenant administrators and members", "tenant configuration, membership records, administrative actions", "none (internal processing)",
            false, null, "Tenant lifetime plus 180 days", "tenant isolation enforcement, administrative audit trail"),
        new("Billing and payments", "Subscription billing, payment processing and invoice issuance", "contract performance",
            "Paying customers", "billing address, payment instrument tokens, transaction records", "payment service providers, tax authorities",
            true, "Payment service provider jurisdictions", "10 years (tax retention)", "PCI DSS scope isolation, tokenized instruments, webhook signature verification"),
        new("Learning delivery", "Course enrollment, progress tracking, assessment and certification", "contract performance",
            "Enrolled learners", "enrollment records, progress data, assessment results, certificate identity", "tenant instructors",
            false, null, "Enrollment lifetime plus 5 years (certification evidence)", "FERPA-aligned access controls, verification without PII exposure"),
        new("Consent management", "Recording and honouring consent for optional processing", "consent",
            "Platform users", "consent state, consent timestamp, collection method", "none (internal processing)",
            false, null, "3 years after consent lapse", "immutable consent records, revocation honoured platform-wide"),
        new("Data subject request handling", "Responding to access, rectification, erasure, portability, restriction and objection requests", "legal obligation",
            "Data subjects", "request records, identity verification metadata, response evidence", "none (internal processing)",
            false, null, "3 years after closure", "30-day statutory deadline tracking, audited lifecycle transitions"),
        new("Security event monitoring", "Detecting and investigating security events and policy violations", "legitimate interest",
            "Platform users and administrators", "authentication events, authorization decisions, security alerts, IP addresses", "security monitoring providers (SIEM forwarding)",
            true, "Configured SIEM forwarding destinations", "400 days (security log retention baseline)", "tamper-evident hash chains, spool-and-forward durability, alert workflow"),
        new("Field-level access auditing", "Monitoring access to personal and sensitive data for privacy compliance", "legal obligation",
            "Data subjects whose records are accessed", "field access observations, sensitivity classifications, anomaly findings", "privacy officers, auditors",
            false, null, "Sensitive observations 365 days (configurable)", "automatic masking, classification-driven retention, access-pattern anomaly detection"),
        new("Audit trail and compliance evidence", "Maintaining audit trails and producing regulatory evidence packages", "legal obligation",
            "Platform users", "audit events, actor identity, tenant context", "auditors, regulators (on request)",
            true, "Auditor jurisdictions on request", "7 years (regulatory baseline)", "ECDSA-signed evidence packages, hash-chained audit logs, chain-of-custody tracking")
    ];

    public async Task<IReadOnlyList<GdprProcessingActivity>> GetActivitiesAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var query = context.Set<GdprProcessingActivity>().AsQueryable();
        if (tenantId.HasValue)
        {
            query = query.Where(activity => activity.TenantId == tenantId.Value || activity.TenantId == null);
        }

        var activities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        return activities
            .OrderBy(activity => activity.ActivityName, StringComparer.Ordinal)
            .ThenBy(activity => activity.Id)
            .ToList();
    }

    public async Task<GdprProcessingActivity?> GetActivityAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Set<GdprProcessingActivity>()
            .FirstOrDefaultAsync(activity => activity.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<GdprProcessingActivity> SaveActivityAsync(GdprProcessingActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        var existing = await GetActivityAsync(activity.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            context.Set<GdprProcessingActivity>().Add(activity);
        }
        else
        {
            existing.Update(
                activity.Purpose, activity.LegalBasis, activity.CategoriesOfDataSubjects, activity.CategoriesOfPersonalData,
                activity.Recipients, activity.InvolvesCrossBorderTransfers, activity.TransferDestinations,
                activity.RetentionPeriod, activity.SecurityMeasures,
                timeProvider.GetUtcNow().UtcDateTime);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing ?? activity;
    }

    public async Task<bool> DeleteActivityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var activity = await GetActivityAsync(id, cancellationToken).ConfigureAwait(false);
        if (activity is null)
        {
            return false;
        }

        context.Set<GdprProcessingActivity>().Remove(activity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<GdprProcessingActivity?> MarkReviewedAsync(Guid id, Guid reviewerUserId, CancellationToken cancellationToken = default)
    {
        var activity = await GetActivityAsync(id, cancellationToken).ConfigureAwait(false);
        if (activity is null)
        {
            return null;
        }

        activity.MarkReviewed(reviewerUserId, timeProvider.GetUtcNow().UtcDateTime);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return activity;
    }

    public async Task<int> SeedPlatformActivitiesAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await context.Set<GdprProcessingActivity>()
            .Where(activity => activity.TenantId == tenantId && activity.IsAutoDerived)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var existingByName = existing.ToDictionary(activity => activity.ActivityName, StringComparer.Ordinal);

        var created = 0;
        foreach (var template in PlatformCatalog)
        {
            if (existingByName.ContainsKey(template.ActivityName))
            {
                continue;
            }

            context.Set<GdprProcessingActivity>().Add(GdprProcessingActivity.Create(
                tenantId,
                template.ActivityName,
                template.Purpose,
                template.LegalBasis,
                template.CategoriesOfDataSubjects,
                template.CategoriesOfPersonalData,
                template.Recipients,
                template.InvolvesCrossBorderTransfers,
                template.TransferDestinations,
                template.RetentionPeriod,
                template.SecurityMeasures,
                isAutoDerived: true,
                createdAt: now));
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return created;
    }

    public async Task<IReadOnlyList<string>> GetCompletenessGapsAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var activities = await GetActivitiesAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var gaps = new List<string>();
        foreach (var activity in activities)
        {
            if (string.IsNullOrWhiteSpace(activity.Purpose))
            {
                gaps.Add($"{activity.ActivityName}: purpose is missing (Article 30(1)(a)).");
            }
            if (string.IsNullOrWhiteSpace(activity.CategoriesOfDataSubjects))
            {
                gaps.Add($"{activity.ActivityName}: categories of data subjects are missing (Article 30(1)(c)).");
            }
            if (string.IsNullOrWhiteSpace(activity.CategoriesOfPersonalData))
            {
                gaps.Add($"{activity.ActivityName}: categories of personal data are missing (Article 30(1)(c)).");
            }
            if (string.IsNullOrWhiteSpace(activity.Recipients))
            {
                gaps.Add($"{activity.ActivityName}: recipients are missing (Article 30(1)(d)).");
            }
            if (string.IsNullOrWhiteSpace(activity.RetentionPeriod))
            {
                gaps.Add($"{activity.ActivityName}: retention period is missing (Article 30(1)(f)).");
            }
            if (string.IsNullOrWhiteSpace(activity.SecurityMeasures))
            {
                gaps.Add($"{activity.ActivityName}: security measures are missing (Article 30(1)(g)).");
            }
            if (activity.InvolvesCrossBorderTransfers && string.IsNullOrWhiteSpace(activity.TransferDestinations))
            {
                gaps.Add($"{activity.ActivityName}: cross-border transfer declared without destinations (Article 30(1)(e)).");
            }
            if (!activity.LastReviewedAtUtc.HasValue)
            {
                gaps.Add($"{activity.ActivityName}: never reviewed.");
            }
        }

        return gaps;
    }
}
