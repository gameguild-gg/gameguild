using GameGuild;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Audit;

/// <summary>One (entity, field) read observation inside a batch: the observed value is used only for classification.</summary>
public sealed record FieldReadObservation(Guid EntityId, string FieldName, string? Value);

/// <summary>
/// Field-level data access auditing service (issue #186): records individual field read/write/export
/// observations with automatic sensitive-field classification and privacy-safe masked values, and
/// answers field-access history queries for investigations. Every recorded observation upserts the
/// sensitive-field classification registry so the classification system stays synchronized with the
/// data actually flowing through the platform.
/// </summary>
public sealed class FieldAccessAuditService(
    IApplicationDbContext context,
    TimeProvider timeProvider) : IFieldAccessAuditService
{
    public async Task<Result<FieldAccessAudit>> RecordFieldAccessAsync(
        Guid tenantId,
        Guid userId,
        string entityType,
        Guid entityId,
        string fieldName,
        FieldAccessType accessType,
        string? oldValue,
        string? newValue,
        bool isSensitive,
        SensitivityLevel sensitivityLevel,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        var effectiveSensitivity = isSensitive ? sensitivityLevel : SensitivityLevel.Public;
        var classification = SensitiveFieldClassifier.Classify(entityType, fieldName, newValue ?? oldValue);
        if (classification is { IsSensitive: true })
        {
            // Classification is authoritative for sensitivity: a caller may not downgrade a field
            // the classifier recognizes as sensitive.
            effectiveSensitivity = classification.Sensitivity;
            await UpsertClassificationAsync(tenantId, entityType, fieldName, classification, cancellationToken).ConfigureAwait(false);
        }

        var entry = FieldAccessAudit.Create(
            tenantId,
            userId,
            entityType,
            entityId,
            fieldName,
            accessType,
            classification is { IsSensitive: true },
            effectiveSensitivity,
            ipAddress ?? string.Empty,
            userAgent ?? string.Empty);
        entry.SetValues(
            oldValue,
            newValue,
            string.IsNullOrWhiteSpace(oldValue) ? null : SensitiveFieldClassifier.Mask(oldValue, effectiveSensitivity),
            string.IsNullOrWhiteSpace(newValue) ? null : SensitiveFieldClassifier.Mask(newValue, effectiveSensitivity));

        context.Set<FieldAccessAudit>().Add(entry);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(entry);
    }

    /// <summary>Records a read-path field observation with ambient actor and request context.</summary>
    public async Task<Result<FieldAccessAudit>> RecordFieldReadAsync(
        Guid tenantId,
        Guid userId,
        string entityType,
        Guid entityId,
        IEnumerable<string> fieldNames,
        string? ipAddress,
        string? userAgent,
        string? requestId = null,
        string? sessionId = null,
        string? apiEndpoint = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);
        var created = new List<FieldAccessAudit>();
        foreach (var fieldName in fieldNames)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                continue;
            }

            var result = await RecordFieldAccessAsync(
                tenantId, userId, entityType, entityId, fieldName,
                FieldAccessType.Read, null, null,
                isSensitive: false,
                sensitivityLevel: SensitivityLevel.Public,
                ipAddress ?? string.Empty, userAgent ?? string.Empty,
                cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                var entry = result.Value;
                entry.SetAccessContext(requestId, sessionId, apiEndpoint);
                created.Add(entry);
            }
        }

        return created.Count == 0
            ? Result.Failure<FieldAccessAudit>(Error.Failure("FieldAccessAudit.NoFields", "No field names were supplied."))
            : Result.Success(created[0]);
    }

    /// <summary>
    /// Records a batch of read-path observations for one entity type in a single save: one row per
    /// (entity, field) pair, classified inline by the sensitive-field classifier. Used by the
    /// response read-capture surface, where one request can return many entities. Read observations
    /// never persist field values — only the fact, actor and context of the access.
    /// </summary>
    public async Task<int> RecordFieldReadsAsync(
        Guid tenantId,
        Guid userId,
        string entityType,
        IReadOnlyList<FieldReadObservation> observations,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestId = null,
        string? apiEndpoint = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count == 0)
        {
            return 0;
        }

        var seen = new HashSet<(Guid EntityId, string FieldName)>();
        foreach (var observation in observations)
        {
            if (string.IsNullOrWhiteSpace(observation.FieldName) || !seen.Add((observation.EntityId, observation.FieldName)))
            {
                continue;
            }

            var classification = SensitiveFieldClassifier.Classify(entityType, observation.FieldName, observation.Value);
            var row = FieldAccessAudit.Create(
                tenantId,
                userId,
                entityType,
                observation.EntityId,
                observation.FieldName,
                FieldAccessType.Read,
                classification is { IsSensitive: true },
                classification is { IsSensitive: true } ? classification.Sensitivity : SensitivityLevel.Public,
                ipAddress ?? string.Empty,
                userAgent ?? string.Empty);
            row.SetAccessContext(requestId, null, apiEndpoint);
            context.Set<FieldAccessAudit>().Add(row);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return seen.Count;
    }

    public string MaskSensitiveData(string? value, SensitivityLevel level) =>
        SensitiveFieldClassifier.Mask(value, level);

    /// <summary>Lists the sensitive-field classification registry for a tenant (or the global rows when tenant is null).</summary>
    public async Task<IReadOnlyList<SensitiveFieldClassification>> GetClassificationsAsync(
        Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var query = context.Set<SensitiveFieldClassification>().AsQueryable();
        if (tenantId.HasValue)
        {
            query = query.Where(classification => classification.TenantId == tenantId.Value || classification.TenantId == null);
        }

        var classifications = await query
            .OrderBy(classification => classification.EntityType)
            .ThenBy(classification => classification.FieldName)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return classifications;
    }

    public string RedactPii(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        content = PiiRedactionEngine.GetPresetPattern(PiiPresetPattern.Email).Replace(content, "[redacted:email]");
        content = PiiRedactionEngine.GetPresetPattern(PiiPresetPattern.NationalId).Replace(content, "[redacted:national-id]");
        content = PiiRedactionEngine.GetPresetPattern(PiiPresetPattern.CreditCard).Replace(content, "[redacted:card]");
        return content;
    }

    public async Task<Result<IEnumerable<FieldAccessAudit>>> GetFieldAccessHistoryAsync(
        Guid entityId, string? fieldName = null, CancellationToken cancellationToken = default)
    {
        var query = context.Set<FieldAccessAudit>().Where(access => access.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(fieldName))
        {
            query = query.Where(access => access.FieldName == fieldName);
        }

        var history = await query
            .OrderByDescending(access => access.AccessedAt)
            .Take(500)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IEnumerable<FieldAccessAudit>>(history);
    }

    public async Task<Result<IEnumerable<FieldAccessAudit>>> GetSensitiveFieldAccessesAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var accesses = await context.Set<FieldAccessAudit>()
            .Where(access => access.TenantId == tenantId
                && access.IsSensitiveField
                && access.AccessedAt >= startDate
                && access.AccessedAt <= endDate)
            .OrderByDescending(access => access.AccessedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success<IEnumerable<FieldAccessAudit>>(accesses);
    }

    private async Task UpsertClassificationAsync(
        Guid tenantId,
        string entityType,
        string fieldName,
        SensitiveFieldMatch match,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await context.Set<SensitiveFieldClassification>()
            .FirstOrDefaultAsync(classification =>
                classification.EntityType == entityType
                && classification.FieldName == fieldName
                && classification.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Set<SensitiveFieldClassification>().Add(
                SensitiveFieldClassification.Create(tenantId, entityType, fieldName, match.Category, match.Sensitivity, match.Rule, match.Confidence, now));
            return;
        }

        existing.ObserveAuto(match.Category, match.Sensitivity, match.Rule, match.Confidence, now);
    }
}
