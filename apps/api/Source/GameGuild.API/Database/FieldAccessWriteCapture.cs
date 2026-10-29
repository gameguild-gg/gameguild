using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using GameGuild.API.Eventing;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authorization;

namespace GameGuild.API.Database;

/// <summary>
///     Universal field-level write tracking (issue #186 criterion 1). Runs inside
///     <see cref="ApplicationDbContext.SaveChangesAsync(CancellationToken)"/> immediately after
///     <see cref="EntityChangeAuditCapture"/>: for every tracked Added/Modified/Deleted entity it
///     stages one insert-only <see cref="FieldAccessAudit"/> row per changed field, classified by
///     <see cref="SensitiveFieldClassifier"/> with privacy-safe masked values, actor and tenant
///     resolved from the same trusted operation context as the entity-change record. The pass is
///     O(changed properties) over tracked state with no extra database queries; classification
///     registry synchronization stays with the recording service and the scan endpoint.
/// </summary>
/// <remarks>
///     <para>
///         Sensitive field values are never persisted raw by this pass: rows the classifier marks
///         sensitive store only their masked rendering, so the field-level store itself stays
///         safe to inspect. Non-sensitive values are stored as-is to keep write diffs useful.
///     </para>
///     <para>
///         Observations are tenant-scoped, mirroring the field-level family's shared query surface:
///         writes that resolve to no tenant (global platform state) stay covered by the
///         <see cref="EntityChangeAuditRecord"/> history and produce no field-level rows.
///     </para>
///     <para>
///         Capture is fail-open by design, mirroring the entity-change pass: a capture failure
///         only logs a warning and never prevents an authorized entity write.
///     </para>
/// </remarks>
internal static class FieldAccessWriteCapture
{
    private const string AuditStoreNamespacePrefix = "GameGuild.Compliance.Audit";
    private const int MaxStoredValueLength = 512;

    private static readonly Type[] ExcludedTypes =
    [
        typeof(EntityChangeAuditRecord),
        typeof(FieldAccessAudit),
        typeof(OutboxMessage),
        typeof(InboxReceipt),
        typeof(OutboxDeliveryReceipt),
        typeof(DataProtectionKey),
        typeof(PermissionAuditLog),
        typeof(PermissionEvaluationLogEntry)
    ];

    // Auditing the audit stores themselves would recurse; durable transport and data-protection
    // keys are system-internal write-ahead records, not business entities.
    private static readonly ConcurrentDictionary<Type, bool> AuditStoreTypeCache = new();

    /// <summary>
    ///     Stages field-level access rows for every changed field of all tracked entity changes and
    ///     adds them to the context. Returns the number of rows staged, used to keep the
    ///     <c>SaveChanges</c> row count the caller observes stable.
    /// </summary>
    internal static int Capture(
        DbContext context,
        UseCaseOperationContext? operation,
        ILogger? logger,
        Guid? mutationActorId = null,
        Guid? mutationTenantId = null,
        bool useEntityTenantScope = false)
    {
        var rows = new List<FieldAccessAudit>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)
                || IsAuditStoreEntity(entry.Metadata.ClrType))
            {
                continue;
            }

            try
            {
                var isDeleted = entry.State == EntityState.Deleted;
                var entityName = entry.Metadata.ClrType.Name;
                // Field-level observations are tenant-scoped rows (the family's shared query
                // surface); global-scope writes stay covered by the entity-change record.
                var tenantId = ResolveTenantId(entry, operation, isDeleted, mutationActorId, mutationTenantId, useEntityTenantScope);
                if (tenantId is null)
                {
                    continue;
                }

                var entityId = ReadPrimaryKeyAsGuid(entry, useOriginalValues: isDeleted);
                var actorId = mutationActorId ?? ResolveActorId(entry, operation, isDeleted);
                var correlationId = operation?.CorrelationId.ToString();
                var accessType = entry.State switch
                {
                    EntityState.Deleted => FieldAccessType.Delete,
                    _ => FieldAccessType.Write
                };

                foreach (var property in entry.Properties)
                {
                    // Modified entries record only fields that actually changed; Added and
                    // Deleted entries carry the full field set of their first/last state.
                    if (entry.State == EntityState.Modified && !property.IsModified)
                    {
                        continue;
                    }

                    var fieldName = property.Metadata.Name;
                    var originalValue = entry.State == EntityState.Added ? null : Render(property.OriginalValue);
                    var currentValue = isDeleted ? null : Render(property.CurrentValue);
                    var classification = SensitiveFieldClassifier.Classify(entityName, fieldName, currentValue ?? originalValue);
                    var isSensitive = classification is { IsSensitive: true };
                    var sensitivity = isSensitive ? classification!.Sensitivity : SensitivityLevel.Public;

                    var row = FieldAccessAudit.Create(
                        tenantId.Value,
                        actorId ?? Guid.Empty,
                        entityName,
                        entityId,
                        fieldName,
                        accessType,
                        isSensitive,
                        sensitivity,
                        ipAddress: string.Empty,
                        userAgent: string.Empty);
                    row.SetAccessContext(correlationId, null, null);
                    if (isSensitive)
                    {
                        // Privacy-safe by construction: sensitive values persist only masked.
                        row.SetValues(
                            null,
                            null,
                            string.IsNullOrWhiteSpace(originalValue) ? null : SensitiveFieldClassifier.Mask(originalValue, sensitivity),
                            string.IsNullOrWhiteSpace(currentValue) ? null : SensitiveFieldClassifier.Mask(currentValue, sensitivity));
                    }
                    else
                    {
                        row.SetValues(originalValue, currentValue, originalValue, currentValue);
                    }

                    rows.Add(row);
                }
            }
            catch (Exception exception)
            {
                // Field-level capture must never prevent an authorized entity change.
                logger?.LogWarning(
                    exception,
                    "Could not capture field-level write observations for {EntityType}",
                    entry.Metadata.ClrType.Name);
            }
        }

        if (rows.Count > 0)
        {
            context.Set<FieldAccessAudit>().AddRange(rows);
        }

        return rows.Count;
    }

    private static bool IsAuditStoreEntity(Type clrType) =>
        AuditStoreTypeCache.GetOrAdd(clrType, static type =>
            ExcludedTypes.Contains(type)
            || (type.Namespace?.StartsWith(AuditStoreNamespacePrefix, StringComparison.Ordinal) ?? false)
            || type.Name.Contains("AuditLog", StringComparison.OrdinalIgnoreCase));

    private static string? Render(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var rendered = value switch
        {
            string text => text,
            Guid guid => guid.ToString(),
            DateTimeOffset offset => offset.ToString("O"),
            DateTime utc => utc.ToString("O"),
            _ => value.ToString()
        };
        return rendered is null || rendered.Length <= MaxStoredValueLength
            ? rendered
            : rendered[..MaxStoredValueLength];
    }

    private static Guid ReadPrimaryKeyAsGuid(EntityEntry entry, bool useOriginalValues)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count != 1)
        {
            return Guid.Empty;
        }

        var value = useOriginalValues
            ? entry.Property(primaryKey.Properties[0].Name).OriginalValue
            : entry.Property(primaryKey.Properties[0].Name).CurrentValue;
        return value switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var parsed) => parsed,
            _ => Guid.Empty
        };
    }

    private static Guid? ResolveTenantId(EntityEntry entry, UseCaseOperationContext? operation, bool isDeleted,
        Guid? mutationActorId, Guid? mutationTenantId, bool useEntityTenantScope)
    {
        if (mutationActorId.HasValue)
        {
            var tenant = useEntityTenantScope ? ReadGuidProperty(entry, "TenantId", isDeleted) : mutationTenantId;
            return tenant == Guid.Empty ? null : tenant;
        }

        return ReadGuidProperty(entry, "TenantId", isDeleted)
            ?? (operation?.TenantId is { } operationTenantId && operationTenantId != DurableIntegrationEventTenants.Platform
                ? operationTenantId
                : null);
    }

    private static Guid? ResolveActorId(EntityEntry entry, UseCaseOperationContext? operation, bool isDeleted) =>
        (operation?.ActorId is { } actorId && actorId != Guid.Empty ? actorId : (Guid?)null)
        ?? ReadGuidProperty(entry, "UpdatedByUserId", isDeleted)
        ?? ReadGuidProperty(entry, "CreatedByUserId", isDeleted);

    private static Guid? ReadGuidProperty(EntityEntry entry, string propertyName, bool useOriginalValues)
    {
        var property = entry.Properties.FirstOrDefault(candidate => candidate.Metadata.Name == propertyName);
        if (property is null)
        {
            return null;
        }

        var value = useOriginalValues ? property.OriginalValue : property.CurrentValue;
        return value switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var parsed) => parsed,
            _ => (Guid?)null
        };
    }
}
