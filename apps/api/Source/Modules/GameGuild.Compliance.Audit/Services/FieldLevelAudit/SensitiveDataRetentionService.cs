using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.Audit;

/// <summary>Result of one sensitive-data retention enforcement pass.</summary>
public sealed record SensitiveDataRetentionEnforcementResult(
    DateTime ExecutedAtUtc,
    IReadOnlyList<SensitiveDataCategory> CategoriesEnforced,
    int PurgedFieldAccessRecords,
    int MaskedFieldAccessRecords,
    int ExpiredClassifications,
    bool DryRun);

public interface ISensitiveDataRetentionService
{
    Task<IReadOnlyList<SensitiveDataRetentionPolicy>> GetPoliciesAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<SensitiveDataRetentionPolicy> ConfigureAsync(
        Guid? tenantId, SensitiveDataCategory category, int retentionDays, SensitiveDataRetentionAction action, Guid? updatedByUserId, CancellationToken cancellationToken = default);
    Task<SensitiveDataRetentionEnforcementResult> EnforceAsync(Guid? tenantId, Guid? triggeredByUserId, bool dryRun, CancellationToken cancellationToken = default);
}

/// <summary>
/// Retention enforcement for sensitive data lifecycle management (issue #186): applies per-category
/// retention rules to field-access records — purging or masking observations whose window elapsed —
/// and expires classification registry entries whose fields have not been observed within the window.
/// Rules default from configuration; without a policy row for a category, the configured default
/// retention applies only when a tenant has explicitly opted in by creating the rule.
/// </summary>
public sealed class SensitiveDataRetentionService(
    IApplicationDbContext context,
    IOptions<FieldLevelAuditOptions> options,
    TimeProvider timeProvider) : ISensitiveDataRetentionService
{
    public async Task<IReadOnlyList<SensitiveDataRetentionPolicy>> GetPoliciesAsync(
        Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var query = context.Set<SensitiveDataRetentionPolicy>().AsQueryable();
        if (tenantId.HasValue)
        {
            query = query.Where(policy => policy.TenantId == tenantId.Value || policy.TenantId == null);
        }

        return await query.OrderBy(policy => policy.Category).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<SensitiveDataRetentionPolicy> ConfigureAsync(
        Guid? tenantId,
        SensitiveDataCategory category,
        int retentionDays,
        SensitiveDataRetentionAction action,
        Guid? updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (category == SensitiveDataCategory.None)
        {
            throw new ArgumentOutOfRangeException(nameof(category), "Retention rules apply to sensitive categories only.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await context.Set<SensitiveDataRetentionPolicy>()
            .FirstOrDefaultAsync(policy => policy.TenantId == tenantId && policy.Category == category, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            var created = SensitiveDataRetentionPolicy.Create(tenantId, category, retentionDays, action, updatedByUserId, now);
            context.Set<SensitiveDataRetentionPolicy>().Add(created);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return created;
        }

        existing.Update(retentionDays, action, updatedByUserId, now);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing;
    }

    public async Task<SensitiveDataRetentionEnforcementResult> EnforceAsync(
        Guid? tenantId, Guid? triggeredByUserId, bool dryRun, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var policies = await GetPoliciesAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var tenantPolicies = policies
            .Where(policy => policy.TenantId == tenantId)
            .ToList();

        // Without an explicit tenant rule the pass is a no-op: retention only ever shortens
        // lifetimes a tenant explicitly configured, mirroring SecurityLogRetentionService.
        if (tenantPolicies.Count == 0)
        {
            return new SensitiveDataRetentionEnforcementResult(now, [], 0, 0, 0, dryRun);
        }

        var purged = 0;
        var masked = 0;
        var expiredClassifications = 0;
        var enforcedCategories = new List<SensitiveDataCategory>();

        foreach (var policy in tenantPolicies)
        {
            var cutoff = now.AddDays(-policy.RetentionDays);
            var expiredAccesses = await context.Set<FieldAccessAudit>()
                .Where(access => access.TenantId == tenantId
                    && access.AccessedAt < cutoff
                    && access.IsSensitiveField
                    && access.SensitivityLevel >= SensitivityLevel.Confidential)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            // Only records the classifier marked sensitive carry lifecycle obligations; public
            // observations follow the security log retention baseline instead.
            var categoriesByField = await context.Set<SensitiveFieldClassification>()
                .Where(classification => classification.TenantId == tenantId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var sensitiveFields = categoriesByField
                .Where(classification => classification.Category == policy.Category)
                .Select(classification => classification.EntityType + ":" + classification.FieldName)
                .ToHashSet(StringComparer.Ordinal);
            var inCategory = expiredAccesses
                .Where(access => sensitiveFields.Contains(access.EntityType + ":" + access.FieldName))
                .ToList();

            if (dryRun)
            {
                if (policy.Action == SensitiveDataRetentionAction.Purge)
                {
                    purged += inCategory.Count;
                }
                else
                {
                    masked += inCategory.Count;
                }
            }
            else if (policy.Action == SensitiveDataRetentionAction.Purge)
            {
                context.Set<FieldAccessAudit>().RemoveRange(inCategory);
                purged += inCategory.Count;
            }
            else
            {
                foreach (var access in inCategory)
                {
                    MaskValues(access);
                    masked++;
                }
            }

            var staleClassifications = categoriesByField
                .Where(classification => classification.Category == policy.Category
                    && classification.LastSeenAt < now.AddDays(-Math.Max(policy.RetentionDays, options.Value.DefaultSensitiveRetentionDays)))
                .ToList();
            if (!dryRun)
            {
                context.Set<SensitiveFieldClassification>().RemoveRange(staleClassifications);
                policy.MarkEnforced(
                    inCategory.Count + staleClassifications.Count, now);
            }
            expiredClassifications += staleClassifications.Count;
            enforcedCategories.Add(policy.Category);
        }

        if (!dryRun)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new SensitiveDataRetentionEnforcementResult(
            now, enforcedCategories, purged, masked, expiredClassifications, dryRun);
    }

    private static void MaskValues(FieldAccessAudit access)
    {
        // Masking replaces raw values entirely; the masked columns keep the investigation surface.
        access.SetValues(
            null, null,
            access.MaskedOldValue, access.MaskedNewValue);
    }
}
