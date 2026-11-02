using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.Json;
using GameGuild.API.Eventing;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Database;

/// <summary>
///     Universal audit capture for set-based writes (issue #166 remainder): EF Core
///     <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> (and equivalent single-statement raw
///     <c>UPDATE</c>/<c>DELETE</c> SQL) bypass the change tracker, so the tracked-entity
///     capture in <see cref="EntityChangeAuditCapture"/> never sees them. This interceptor
///     detects those commands through the command source reported by EF Core
///     (<c>ExecuteUpdate</c>/<c>ExecuteDelete</c>/raw SQL), replays
///     their row-selection predicate on the same connection and transaction to read the
///     before-image (and, for updates, the after-image by primary key), and stages
///     insert-only <see cref="EntityChangeAuditRecord"/> rows — plus forensic
///     <see cref="MutationValueSnapshot"/> rows when the snapshot factory is available —
///     on the executing context so they persist, pass through the centralized PII
///     redaction pipeline, and commit or roll back together with the next
///     <c>SaveChanges</c> call of the same transaction. The guarded business statement
///     itself is never modified: counter limits, idempotency and affected-row counts stay
///     exactly as the caller issued them.
/// </summary>
/// <remarks>
///     <para>
///         Capture is fail-open by design, mirroring the tracked-entity pass: every
///         interception callback is exception-guarded and a capture failure only logs a
///         warning — an authorized set-based mutation is never blocked by audit capture.
///         Statements whose shape cannot be replayed safely (CTEs, joins, RETURNING,
///         multi-statement batches, output parameters) are skipped with a warning.
///     </para>
///     <para>
///         The before-image read appends <c>FOR UPDATE</c> on PostgreSQL so the read rows
///         are locked until the intercepted mutation executes in the same transaction;
///         the before/after pair is therefore transactionally accurate under concurrent
///         writers. The after-image for updates is re-read by primary key after the
///         mutation, which keeps guarded updates honest: rows selected by the replayed
///         predicate but left unchanged (for example because a concurrent redemption
///         already moved a counter) produce no after-image difference and are skipped,
///         while genuinely mutated rows always produce exactly one record each.
///     </para>
/// </remarks>
internal sealed class SetBasedChangeAuditInterceptor(
    IUseCaseOperationContextAccessor operationAccessor,
    IOptions<EntityChangeAuditOptions> options,
    MutationValueSnapshotFactory? snapshotFactory = null,
    ILogger<SetBasedChangeAuditInterceptor>? logger = null) : DbCommandInterceptor
{
    private const string RedactedValue = EntityChangeAuditCapture.RedactedValue;
    private const string AuditStoreNamespacePrefix = "GameGuild.Compliance.Audit";

    private static readonly Type[] ExcludedTypes =
    [
        typeof(EntityChangeAuditRecord),
        typeof(OutboxMessage),
        typeof(InboxReceipt),
        typeof(OutboxDeliveryReceipt),
        typeof(Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey),
        typeof(PermissionAuditLog),
        typeof(PermissionEvaluationLogEntry)
    ];

    private static readonly ConcurrentDictionary<IModel, ConcurrentDictionary<string, TableAuditMapping?>> TableMappingCache = new();
    private static readonly ConcurrentDictionary<IProperty, bool> SensitivePropertyCache = new();

    private readonly ConditionalWeakTable<DbCommand, PendingCapture> _pending = new();

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        CaptureBeforeState(command, eventData);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    {
        FinalizeCapture(command, eventData, result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (ShouldCapture(eventData, out var statement, out var mapping))
            {
                return new ValueTask<InterceptionResult<int>>(
                    CaptureBeforeAsync(command, eventData, statement, mapping, result));
            }
        }
        catch (Exception exception)
        {
            // Fail-open: audit capture must never block an authorized set-based mutation.
            logger?.LogWarning(exception, "Set-based audit before-image capture failed and was skipped.");
        }

        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_pending.TryGetValue(command, out var pending))
            {
                _pending.Remove(command);
                return new ValueTask<int>(FinalizeAsync(command, eventData, pending, result));
            }
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Set-based audit staging failed and was skipped.");
        }

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        CaptureBeforeState(command, eventData);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (ShouldCapture(eventData, out var statement, out var mapping))
            {
                return new ValueTask<InterceptionResult<DbDataReader>>(
                    CaptureBeforeReaderAsync(command, eventData, statement, mapping, result));
            }
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Set-based audit before-image capture failed and was skipped.");
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult DataReaderDisposing(
        DbCommand command, DataReaderDisposingEventData eventData, InterceptionResult dataReaderDisposing)
    {
        // Providers that stream ExecuteUpdate results through a reader dispose that reader
        // through this sync-only hook (the reader rides on the event data); the connection
        // is free again, so the after-image read and staging run here.
        FinalizeCapture(command, eventData, result: null);
        return base.DataReaderDisposing(command, eventData, dataReaderDisposing);
    }

    /// <inheritdoc />
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _pending.Remove(command);
        base.CommandFailed(command, eventData);
    }

    /// <inheritdoc />
    public override Task CommandFailedAsync(
        DbCommand command, CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pending.Remove(command);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private async Task<InterceptionResult<int>> CaptureBeforeAsync(
        DbCommand command, CommandEventData eventData,
        SetBasedSqlParser.ParsedStatement statement, TableAuditMapping mapping,
        InterceptionResult<int> result)
    {
        try
        {
            var rows = await SelectRowsAsync(command, statement, mapping, lockRows: true, keys: null)
                .ConfigureAwait(false);
            StagePending(command, statement, mapping, rows);
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Set-based audit before-image capture failed and was skipped.");
        }

        return result;
    }

    private async Task<InterceptionResult<DbDataReader>> CaptureBeforeReaderAsync(
        DbCommand command, CommandEventData eventData,
        SetBasedSqlParser.ParsedStatement statement, TableAuditMapping mapping,
        InterceptionResult<DbDataReader> result)
    {
        try
        {
            var rows = await SelectRowsAsync(command, statement, mapping, lockRows: true, keys: null)
                .ConfigureAwait(false);
            StagePending(command, statement, mapping, rows);
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Set-based audit before-image capture failed and was skipped.");
        }

        return result;
    }

    private async Task<int> FinalizeAsync(
        DbCommand command, CommandEventData eventData, PendingCapture pending, int affectedRows)
    {
        try
        {
            if (eventData.Context is { } context && affectedRows is not 0)
            {
                var afterRows = pending.Statement.IsUpdate
                    ? await SelectRowsAsync(command, pending.Statement, pending.Mapping, lockRows: false, keys: pending.Keys)
                        .ConfigureAwait(false)
                    : [];
                StageRecords(context, pending, afterRows);
            }
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Set-based audit staging failed and was skipped.");
        }

        return affectedRows;
    }

    private void StagePending(
        DbCommand command, SetBasedSqlParser.ParsedStatement statement, TableAuditMapping mapping,
        List<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0)
        {
            // Guarded statements that match no rows mutate nothing and must not audit.
            return;
        }

        var maxRows = options.Value.MaxSetBasedCapturedRows;
        if (rows.Count > maxRows)
        {
            logger?.LogWarning(
                "Set-based audit capture truncated for {EntityType}: {RowCount} matched rows exceed the configured cap {MaxRows}.",
                mapping.EntityName, rows.Count, maxRows);
            rows = rows.Take(maxRows).ToList();
        }

        _pending.AddOrUpdate(command, new PendingCapture(statement, mapping, rows));
    }

    private void CaptureBeforeState(DbCommand command, CommandEventData eventData)
    {
        try
        {
            if (!ShouldCapture(eventData, out var statement, out var mapping))
            {
                return;
            }

            var rows = SelectRows(command, statement, mapping, lockRows: true, keys: null);
            StagePending(command, statement, mapping, rows);
        }
        catch (Exception exception)
        {
            // Fail-open: audit capture must never block an authorized set-based mutation.
            logger?.LogWarning(exception, "Set-based audit before-image capture failed and was skipped.");
        }
    }

    private void FinalizeCapture(DbCommand command, DbContextEventData eventData, int? result)
    {
        try
        {
            if (!_pending.TryGetValue(command, out var pending))
            {
                return;
            }

            _pending.Remove(command);
            if (eventData.Context is not { } context || result is 0)
            {
                // The guarded statement matched nothing after all (for example a concurrent
                // redemption exhausted the limit between our read and the mutation).
                return;
            }

            var afterRows = pending.Statement.IsUpdate
                ? SelectRows(command, pending.Statement, pending.Mapping, lockRows: false, keys: pending.Keys)
                : [];
            StageRecords(context, pending, afterRows);
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Set-based audit staging failed and was skipped.");
        }
    }

    private bool ShouldCapture(CommandEventData eventData, out SetBasedSqlParser.ParsedStatement statement, out TableAuditMapping mapping)
    {
        statement = null!;
        mapping = null!;
        if (!options.Value.Enabled || !options.Value.CaptureSetBasedChanges)
        {
            return false;
        }

        if (eventData.Context is not { } context)
        {
            return false;
        }

        // EF Core reports ExecuteUpdate/ExecuteDelete statements under CommandSource.BulkUpdate
        // (verified against EF Core 10.0.9 + Npgsql 10.0.2 on PostgreSQL; Npgsql still emits the
        // legacy value although the enum marks it obsolete), so both the legacy and the
        // finer-grained values are matched, plus raw SQL writes.
#pragma warning disable CS0618 // BulkUpdate is obsolete but Npgsql 10.x still reports it for set-based statements
        if (eventData.CommandSource is not (CommandSource.ExecuteUpdate
            or CommandSource.ExecuteDelete
            or CommandSource.BulkUpdate
            or CommandSource.ExecuteSqlRaw))
        {
            return false;
        }
#pragma warning restore CS0618

        statement = SetBasedSqlParser.TryParse(eventData.Command.CommandText)!;
        if (statement is null)
        {
            return false;
        }

        mapping = ResolveMapping(context, statement)!;
        if (mapping is null || mapping.Excluded)
        {
            return false;
        }

        foreach (DbParameter parameter in eventData.Command.Parameters)
        {
            if (parameter.Direction is not System.Data.ParameterDirection.Input)
            {
                // Statements with output parameters cannot be replayed verbatim.
                return false;
            }
        }

        return true;
    }

    /// <summary>Builds the audit records plus snapshots and stages them on the executing context.</summary>
    private void StageRecords(DbContext context, PendingCapture pending, IReadOnlyList<Dictionary<string, object?>> afterRows)
    {
        var operation = operationAccessor.Current;
        var afterByKey = new Dictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal);
        foreach (var row in afterRows)
        {
            afterByKey[RowKey(pending.Mapping, row)] = row;
        }

        var records = new List<EntityChangeAuditRecord>();
        var snapshots = new List<MutationValueSnapshot>();
        var snapshotStagingEnabled = snapshotFactory is not null
            && context.Model.FindEntityType(typeof(MutationValueSnapshot)) is not null;
        var occurredAt = DateTimeOffset.UtcNow;

        foreach (var beforeRow in pending.Rows)
        {
            var keyId = RowKey(pending.Mapping, beforeRow);
            var afterRow = pending.Statement.IsUpdate ? afterByKey.GetValueOrDefault(keyId) : null;
            if (pending.Statement.IsUpdate && afterRow is null)
            {
                // The row disappeared between the mutation and the after-image read (cascade,
                // trigger): the write is evidenced by the before-image only when it still exists.
                logger?.LogWarning(
                    "Set-based audit after-image missing for {EntityType} key {KeyId}; record skipped.",
                    pending.Mapping.EntityName, keyId);
                continue;
            }

            var beforeJson = SerializeRow(pending.Mapping, beforeRow);
            var afterJson = afterRow is null ? null : SerializeRow(pending.Mapping, afterRow);
            var tenantId = ReadGuidColumn(pending.Mapping.TenantColumnName, beforeRow)
                ?? (operation?.TenantId is { } tenant && tenant != DurableIntegrationEventTenants.Platform ? tenant : null);
            Guid? actorId = operation?.ActorId is { } operationActor && operationActor != Guid.Empty
                ? operationActor
                : ReadGuidColumn(pending.Mapping.UpdatedByColumnName, beforeRow)
                  ?? ReadGuidColumn(pending.Mapping.CreatedByColumnName, beforeRow);

            var record = new EntityChangeAuditRecord
            {
                Id = Guid.NewGuid(),
                EntityName = pending.Mapping.EntityName,
                KeyId = keyId,
                ChangeKind = pending.Statement.IsUpdate ? EntityState.Modified.ToString() : EntityState.Deleted.ToString(),
                BeforeValues = beforeJson,
                AfterValues = afterJson,
                TenantId = tenantId,
                ActorId = actorId,
                CorrelationId = operation?.CorrelationId.ToString(),
                OccurredAtUtc = occurredAt
            };
            records.Add(record);

            if (snapshotStagingEnabled)
            {
                var snapshot = snapshotFactory!.Create(
                    record.EntityName, keyId, record.ChangeKind, beforeJson, afterJson,
                    tenantId, actorId, record.CorrelationId, occurredAt);
                if (snapshot is not null)
                {
                    snapshots.Add(snapshot);
                }
            }
        }

        if (records.Count == 0)
        {
            return;
        }

        if (context is ApplicationDbContext applicationDbContext)
        {
            applicationDbContext.StageSetBasedAuditRecords(records, snapshots);
        }
        else
        {
            context.Set<EntityChangeAuditRecord>().AddRange(records);
            if (snapshots.Count > 0)
            {
                context.Set<MutationValueSnapshot>().AddRange(snapshots);
            }
        }

        logger?.LogDebug(
            "Captured {Count} set-based {ChangeKind} change(s) for {EntityType}.",
            records.Count, pending.Statement.IsUpdate ? "update" : "delete", pending.Mapping.EntityName);
    }

    /// <summary>Reads the rows selected by the replayed predicate (or by primary keys) on the same connection and transaction.</summary>
    private List<Dictionary<string, object?>> SelectRows(
        DbCommand command, SetBasedSqlParser.ParsedStatement statement, TableAuditMapping mapping,
        bool lockRows, IReadOnlyList<object?[]>? keys)
    {
        using var select = BuildSelectCommand(command, statement, mapping, lockRows, keys);
        using var reader = select.ExecuteReader();
        return ReadRows(reader, mapping);
    }

    private async Task<List<Dictionary<string, object?>>> SelectRowsAsync(
        DbCommand command, SetBasedSqlParser.ParsedStatement statement, TableAuditMapping mapping,
        bool lockRows, IReadOnlyList<object?[]>? keys)
    {
        await using var select = BuildSelectCommand(command, statement, mapping, lockRows, keys);
        await using var reader = await select.ExecuteReaderAsync().ConfigureAwait(false);
        return ReadRows(reader, mapping);
    }

    private DbCommand BuildSelectCommand(
        DbCommand command, SetBasedSqlParser.ParsedStatement statement, TableAuditMapping mapping,
        bool lockRows, IReadOnlyList<object?[]>? keys)
    {
        var select = command.Connection!.CreateCommand();
        select.Transaction = command.Transaction;
        select.CommandTimeout = command.CommandTimeout;

        var qualifier = statement.Alias is { } alias ? SetBasedSqlParser.Quote(alias) : FormatTableReference(statement);
        var columns = string.Join(", ", mapping.Columns.Select(column => qualifier + "." + SetBasedSqlParser.Quote(column.ColumnName)));

        string filter;
        if (keys is not null)
        {
            // After-image read by primary key: guarded predicates no longer match updated rows.
            var parameterIndex = 0;
            var groups = new List<string>(keys.Count);
            foreach (var keyValues in keys)
            {
                var comparisons = new List<string>(mapping.KeyColumns.Count);
                foreach (var (keyColumn, keyValue) in mapping.KeyColumns.Zip(keyValues, (column, value) => (column, value)))
                {
                    var parameterName = "sbk_" + parameterIndex++;
                    var parameter = select.CreateParameter();
                    parameter.ParameterName = parameterName;
                    parameter.Value = keyValue ?? DBNull.Value;
                    select.Parameters.Add(parameter);
                    comparisons.Add(qualifier + "." + SetBasedSqlParser.Quote(keyColumn.ColumnName) + " = @" + parameterName);
                }

                groups.Add("(" + string.Join(" AND ", comparisons) + ")");
            }

            filter = groups.Count == 0 ? "FALSE" : string.Join(" OR ", groups);
        }
        else if (statement.WhereClause is { } whereClause)
        {
            filter = whereClause;
            foreach (DbParameter parameter in command.Parameters)
            {
                var clone = select.CreateParameter();
                clone.ParameterName = parameter.ParameterName;
                clone.Value = parameter.Value;
                select.Parameters.Add(clone);
            }
        }
        else
        {
            filter = "TRUE";
        }

        select.CommandText = "SELECT " + columns + " FROM " + FormatTableReference(statement)
            + (statement.Alias is { } tableAlias ? " AS " + SetBasedSqlParser.Quote(tableAlias) : string.Empty)
            + " WHERE " + filter
            + (lockRows && IsPostgreSql(command) ? " FOR UPDATE" : string.Empty);
        return select;
    }

    private static List<Dictionary<string, object?>> ReadRows(DbDataReader reader, TableAuditMapping mapping)
    {
        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(mapping.Columns.Count, StringComparer.Ordinal);
            foreach (var column in mapping.Columns)
            {
                var ordinal = reader.GetOrdinal(column.ColumnName);
                var value = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
                row[column.PropertyName] = NormalizeValue(value, column.ClrType);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string FormatTableReference(SetBasedSqlParser.ParsedStatement statement) =>
        string.Join(".", statement.TableIdentifiers.Select(SetBasedSqlParser.Quote));

    private static bool IsPostgreSql(DbCommand command) =>
        command.Connection?.GetType().Name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    private static string RowKey(TableAuditMapping mapping, Dictionary<string, object?> row)
    {
        var values = mapping.KeyColumns
            .Select(column => row.GetValueOrDefault(column.PropertyName)?.ToString() ?? "unknown")
            .ToArray();
        return string.Join(":", values);
    }

    private static string? SerializeRow(TableAuditMapping mapping, Dictionary<string, object?> row)
    {
        var values = new Dictionary<string, object?>(row.Count, StringComparer.Ordinal);
        foreach (var column in mapping.Columns)
        {
            values[column.PropertyName] = column.Sensitive
                ? RedactedValue
                : row.GetValueOrDefault(column.PropertyName);
        }

        return SafeSerialize(values);
    }

    private static string SafeSerialize(Dictionary<string, object?> values)
    {
        try
        {
            return JsonSerializer.Serialize(values);
        }
        catch (Exception)
        {
            var fallback = values.ToDictionary(
                pair => pair.Key,
                pair => pair.Value?.ToString() ?? (object?)"null",
                StringComparer.Ordinal);
            return JsonSerializer.Serialize(fallback);
        }
    }

    private static Guid? ReadGuidColumn(string? columnName, Dictionary<string, object?> row)
    {
        if (columnName is null || !row.TryGetValue(columnName, out var value))
        {
            return null;
        }

        return value switch
        {
            Guid guid => guid == Guid.Empty ? null : guid,
            string text => Guid.TryParse(text, out var parsed) && parsed != Guid.Empty ? parsed : null,
            _ => null
        };
    }

    /// <summary>Converts a provider value to the property's CLR representation so JSON snapshots match the tracked capture.</summary>
    private static object? NormalizeValue(object? value, Type clrType)
    {
        if (value is null)
        {
            return null;
        }

        var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;
        try
        {
            if (underlying.IsEnum)
            {
                return value is string name ? Enum.Parse(underlying, name) : Enum.ToObject(underlying, value);
            }

            if (underlying == typeof(Guid) && value is string guidText && Guid.TryParse(guidText, out var guid))
            {
                return guid;
            }

            if (underlying == typeof(string))
            {
                return Convert.ToString(value);
            }
        }
        catch (Exception)
        {
            // Degrade to the raw provider value; the serializer falls back to ToString.
        }

        return value;
    }

    private TableAuditMapping? ResolveMapping(DbContext context, SetBasedSqlParser.ParsedStatement statement)
    {
        var table = statement.TableIdentifiers[^1];
        var schema = statement.TableIdentifiers.Count == 2 ? statement.TableIdentifiers[0] : null;
        var modelCache = TableMappingCache.GetOrAdd(
            context.Model,
            static _ => new ConcurrentDictionary<string, TableAuditMapping?>(StringComparer.Ordinal));
        var key = (schema ?? string.Empty) + "|" + table;
        return modelCache.GetOrAdd(
            key,
            static (cacheKey, state) => BuildMapping(state.Model, cacheKey, state.Options),
            (Model: context.Model, Options: options.Value));
    }

    private static TableAuditMapping? BuildMapping(IModel model, string key, EntityChangeAuditOptions options)
    {
        var separator = key.IndexOf('|');
        var schema = separator == 0 ? null : key[..separator];
        var table = key[(separator + 1)..];

        var candidates = model.GetEntityTypes()
            .Where(entityType =>
                string.Equals(entityType.GetTableName(), table, StringComparison.Ordinal)
                && string.Equals(entityType.GetSchema(), schema, StringComparison.Ordinal))
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var principal = candidates.OrderBy(entityType => entityType.Name, StringComparer.Ordinal).First();
        var excluded = IsExcludedType(principal.ClrType, options);
        var columns = new List<AuditColumn>();
        IProperty? tenantProperty = null;
        IProperty? updatedByProperty = null;
        IProperty? createdByProperty = null;

        foreach (var entityType in candidates)
        {
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(StoreObjectIdentifier.Table(table, schema));
                if (columnName is null)
                {
                    continue;
                }

                if (columns.All(column => column.PropertyName != property.Name))
                {
                    var sensitive = property.PropertyInfo is not null
                        && SensitivePropertyCache.GetOrAdd(
                            property,
                            static cached => Attribute.IsDefined(cached.PropertyInfo!, typeof(AuditSensitiveAttribute), inherit: true));
                    columns.Add(new AuditColumn(
                        property.Name,
                        columnName,
                        property.ClrType,
                        sensitive));
                }

                tenantProperty ??= property.Name == "TenantId" ? property : null;
                updatedByProperty ??= property.Name == "UpdatedByUserId" ? property : null;
                createdByProperty ??= property.Name == "CreatedByUserId" ? property : null;
            }
        }

        var primaryKey = principal.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count == 0)
        {
            return null;
        }

        var keyColumns = primaryKey.Properties
            .Select(property => columns.FirstOrDefault(column => column.PropertyName == property.Name))
            .Where(column => column is not null)
            .Select(column => column!)
            .ToList();
        if (keyColumns.Count != primaryKey.Properties.Count)
        {
            return null;
        }

        return new TableAuditMapping(
            principal.ClrType.Name,
            excluded,
            columns,
            keyColumns,
            tenantProperty?.Name,
            updatedByProperty?.Name,
            createdByProperty?.Name);
    }

    private static bool IsExcludedType(Type clrType, EntityChangeAuditOptions options) =>
        ExcludedTypes.Contains(clrType)
        || (clrType.Namespace?.StartsWith(AuditStoreNamespacePrefix, StringComparison.Ordinal) ?? false)
        || clrType.Name.Contains("AuditLog", StringComparison.OrdinalIgnoreCase)
        || options.ExcludedEntityTypes.Contains(clrType.Name)
        || (clrType.FullName is { } fullName && options.ExcludedEntityTypes.Contains(fullName));

    private sealed record PendingCapture(
        SetBasedSqlParser.ParsedStatement Statement,
        TableAuditMapping Mapping,
        IReadOnlyList<Dictionary<string, object?>> Rows)
    {
        public IReadOnlyList<object?[]> Keys =>
            Rows.Select(row => Mapping.KeyColumns
                    .Select(column => row.GetValueOrDefault(column.PropertyName))
                    .ToArray())
                .ToList();
    }

    /// <summary>One mapped property of an audited table with its store column and sensitivity.</summary>
    private sealed record AuditColumn(string PropertyName, string ColumnName, Type ClrType, bool Sensitive);

    /// <summary>Model-level mapping of one database table to audit-relevant property metadata.</summary>
    private sealed record TableAuditMapping(
        string EntityName,
        bool Excluded,
        IReadOnlyList<AuditColumn> Columns,
        IReadOnlyList<AuditColumn> KeyColumns,
        string? TenantColumnName,
        string? UpdatedByColumnName,
        string? CreatedByColumnName);
}
