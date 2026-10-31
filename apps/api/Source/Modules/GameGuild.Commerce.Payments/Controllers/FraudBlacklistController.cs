using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Fraud blacklist administration (issue #409): lists, checks, adds and removes the
///     tenant's blacklist entries and manual whitelist overrides. The tenant scope always
///     comes from the actor context, never from the route or body. Authorization is
///     additionally enforced per request through the <c>payments:admin</c> permission on
///     the underlying queries and commands.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fraud/blacklist")]
[Microsoft.AspNetCore.Http.Tags("fraud/blacklist")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
public sealed class FraudBlacklistController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    /// <summary>
    ///     List the tenant's fraud blacklist entries, newest first.
    /// </summary>
    /// <param name="entryType">Optional entry-type filter (IpAddress, EmailDomain, Email, CardBin, DeviceFingerprint, PaymentMethodToken, CardFingerprint, ExternalCustomerId).</param>
    /// <param name="includeExpired">When true, expired entries are included.</param>
    /// <param name="page">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("List fraud blacklist entries")]
    [EndpointDescription("Lists the caller's tenant fraud blacklist and whitelist-override entries, newest first, with optional entry-type and expiry filters. Requires the payments:admin permission.")]
    [ProducesResponseType<FraudBlacklistPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] FraudBlacklistEntryType? entryType = null,
        [FromQuery] bool includeExpired = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        return Ok(await sender.Send(
            new ListFraudBlacklistQuery(tenantId, entryType, includeExpired, page, pageSize),
            ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Check whether a concrete value matches the tenant blacklist (same evaluation the
    ///     payment fraud gate performs).
    /// </summary>
    /// <param name="entryType">Entry kind to evaluate.</param>
    /// <param name="value">Concrete value to check (IP address, email domain, BIN, fingerprint, token...).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("check")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Check a value against the fraud blacklist")]
    [EndpointDescription("Evaluates a concrete value (IP address, email domain, card BIN, device fingerprint, payment-method token, card fingerprint or provider customer id) against the caller's tenant blacklist, honoring wildcard patterns, expiry dates and manual whitelist overrides. Requires the payments:admin permission.")]
    [ProducesResponseType<FraudBlacklistCheckResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Check(
        [FromQuery] FraudBlacklistEntryType entryType,
        [FromQuery] string value,
        CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();

        return Ok(await sender.Send(
            new CheckFraudBlacklistQuery(tenantId, entryType, value),
            ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Add a blacklist entry or a manual whitelist override for the caller's tenant.
    /// </summary>
    /// <param name="body">Entry to add.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [EndpointSummary("Add a fraud blacklist entry")]
    [EndpointDescription("Adds a fraud blacklist entry (or a manual whitelist override) for the caller's tenant. Values support * wildcard patterns (e.g. *.malicious.com, 192.168.*) and optional expiry dates. Idempotent per (type, value, kind). Requires the payments:admin permission.")]
    [ProducesResponseType<FraudBlacklistEntryResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Add([FromBody] AddFraudBlacklistEntryRequest body, CancellationToken ct)
    {
        if (!Enum.TryParse<FraudBlacklistEntryType>(body.EntryType, ignoreCase: true, out var entryType))
        {
            return BadRequest(new { error = $"Unknown entryType '{body.EntryType}'." });
        }

        var result = await sender.Send(
            new AddFraudBlacklistEntryCommand(entryType, body.Value, body.Reason, body.IsWhitelist, body.ExpiresAt),
            ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Remove a blacklist entry owned by the caller's tenant.
    /// </summary>
    /// <param name="entryId">Entry to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{entryId:guid}")]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [EndpointSummary("Remove a fraud blacklist entry")]
    [EndpointDescription("Removes (soft-deletes) a fraud blacklist entry owned by the caller's tenant. Requires the payments:admin permission.")]
    [ProducesResponseType<RemoveFraudBlacklistEntryResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid entryId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveFraudBlacklistEntryCommand(entryId), ct).ConfigureAwait(false);

        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    ///     Export the tenant's fraud blacklist entries as CSV (issue #409: import/export blacklists).
    /// </summary>
    /// <param name="entryType">Optional entry-type filter, mirroring <c>GET /fraud/blacklist</c>.</param>
    /// <param name="includeExpired">When true, expired entries are included.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("export")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Export fraud blacklist entries as CSV")]
    [EndpointDescription("Exports the caller's tenant fraud blacklist and whitelist-override entries as CSV (columns: entry_type,value,kind,reason,expires_at,created_at). Wildcard patterns are exported verbatim so a re-import reproduces them. Requires the payments:admin permission.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromQuery] FraudBlacklistEntryType? entryType = null,
        [FromQuery] bool includeExpired = false,
        CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();

        var rows = new List<FraudBlacklistEntryResult>();
        FraudBlacklistPage page;
        var pageNumber = 1;
        do
        {
            page = await sender.Send(
                new ListFraudBlacklistQuery(tenantId, entryType, includeExpired, pageNumber, FraudBlacklistCsv.ExportPageSize),
                ct).ConfigureAwait(false);
            rows.AddRange(page.Items);
            pageNumber++;
        }
        while (rows.Count < page.TotalCount);

        return File(
            FraudBlacklistCsv.Compose(rows),
            "text/csv; charset=utf-8",
            $"fraud-blacklist-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    ///     Import fraud blacklist entries from a CSV request body (issue #409: import/export
    ///     blacklists). Accepts the file shape produced by <c>GET /fraud/blacklist/export</c>
    ///     (header row detected and skipped); every row is added through the same idempotent
    ///     command as the add endpoint, so re-importing is safe. Rows that fail validation
    ///     are reported individually and never abort the rest of the batch.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("import")]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [EndpointSummary("Import fraud blacklist entries from CSV")]
    [EndpointDescription("Imports fraud blacklist entries from a CSV body (columns: entry_type,value,kind,reason,expires_at; header row optional, * wildcards supported). At most 1000 rows and 1 MB per request; idempotent per (type, value, kind). Requires the payments:admin permission.")]
    [ProducesResponseType<FraudBlacklistImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Import(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var csv = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        if (csv.Length > FraudBlacklistCsv.MaxImportCharacters)
        {
            return BadRequest(new { error = $"CSV body exceeds the {FraudBlacklistCsv.MaxImportCharacters:N0}-character limit." });
        }

        List<FraudBlacklistCsv.Row> rows;
        try
        {
            rows = FraudBlacklistCsv.ParseImport(csv);
        }
        catch (FraudBlacklistCsvFormatException exception)
        {
            return BadRequest(new { error = exception.Message });
        }

        var imported = 0;
        var errors = new List<string>();
        foreach (var row in rows)
        {
            try
            {
                await sender.Send(
                    new AddFraudBlacklistEntryCommand(row.EntryType, row.Value, row.Reason, row.IsWhitelist, row.ExpiresAt),
                    ct).ConfigureAwait(false);
                imported++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors.Add($"row {row.LineNumber}: {exception.Message}");
            }
        }

        return Ok(new FraudBlacklistImportResult(imported, rows.Count - imported, errors));
    }

    /// <summary>Summary of a blacklist CSV import.</summary>
    /// <param name="Imported">Rows successfully stored (or already present — the add is idempotent).</param>
    /// <param name="Failed">Rows that were rejected or failed to store.</param>
    /// <param name="Errors">Bounded per-row error messages, one per failed row.</param>
    public sealed record FraudBlacklistImportResult(int Imported, int Failed, IReadOnlyList<string> Errors);

    /// <summary>Request body for adding a blacklist entry.</summary>
    /// <param name="EntryType">Entry kind (IpAddress, EmailDomain, Email, CardBin, DeviceFingerprint, PaymentMethodToken, CardFingerprint, ExternalCustomerId).</param>
    /// <param name="Value">Pattern to store; * wildcards supported.</param>
    /// <param name="Reason">Optional bounded reason.</param>
    /// <param name="IsWhitelist">True for a manual whitelist override.</param>
    /// <param name="ExpiresAt">Optional expiry instant (ISO-8601).</param>
    public sealed record AddFraudBlacklistEntryRequest(
        string EntryType,
        string Value,
        string? Reason = null,
        bool IsWhitelist = false,
        DateTime? ExpiresAt = null);

    private Guid ResolveTenantId()
        => actorContextAccessor.ActorContext.TenantId
           ?? throw new UnauthorizedAccessException("A tenant context is required to manage the fraud blacklist.");
}

/// <summary>
///     CSV serialization for the fraud blacklist (issue #409: import/export blacklists). The
///     export writes RFC 4180-quoted CSV with a fixed column order; the import accepts the
///     same shape back (header row optional), enforces bounded sizes, and parses quoted
///     fields so reasons containing commas or quotes round-trip.
/// </summary>
public static class FraudBlacklistCsv
{
    /// <summary>Page size used while draining the blacklist for an export.</summary>
    public const int ExportPageSize = 100;

    /// <summary>Maximum rows accepted by one import request (issue #409 batches are bounded).</summary>
    public const int MaxImportRows = 1000;

    /// <summary>Maximum characters accepted in an import body.</summary>
    public const int MaxImportCharacters = 1_000_000;

    private const string Header = "entry_type,value,kind,reason,expires_at";

    /// <summary>One parsed import row.</summary>
    /// <param name="LineNumber">1-based source line, for per-row error reporting.</param>
    /// <param name="EntryType">Parsed entry kind.</param>
    /// <param name="Value">Pattern to store (wildcards allowed).</param>
    /// <param name="Reason">Optional bounded reason.</param>
    /// <param name="IsWhitelist">True for a whitelist override row.</param>
    /// <param name="ExpiresAt">Optional expiry instant.</param>
    public sealed record Row(
        int LineNumber,
        FraudBlacklistEntryType EntryType,
        string Value,
        string? Reason,
        bool IsWhitelist,
        DateTime? ExpiresAt);

    /// <summary>Composes the CSV bytes of the exported entries (UTF-8, RFC 4180 quoting).</summary>
    public static byte[] Compose(IEnumerable<FraudBlacklistEntryResult> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var builder = new System.Text.StringBuilder();
        builder.AppendLine(Header);

        foreach (var entry in entries)
        {
            builder.Append(Field(entry.EntryType.ToString())).Append(',');
            builder.Append(Field(entry.Value)).Append(',');
            builder.Append(Field(entry.IsWhitelist ? "whitelist" : "blacklist")).Append(',');
            builder.Append(Field(entry.Reason ?? string.Empty)).Append(',');
            builder.AppendLine(Field(entry.ExpiresAt?.ToString("O") ?? string.Empty));
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    /// <summary>
    ///     Parses an import CSV. The header row is detected case-insensitively and skipped;
    ///     rows are validated individually. Invalid rows throw
    ///     <see cref="FraudBlacklistCsvFormatException"/> with the physical line number.
    /// </summary>
    public static List<Row> ParseImport(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);

        var rows = new List<Row>();
        var sawHeader = false;
        foreach (var (fields, lineNumber) in SplitRecords(csv))
        {
            if (fields.Length == 1 && string.IsNullOrWhiteSpace(fields[0]))
            {
                continue;
            }

            if (!sawHeader && IsHeader(fields))
            {
                sawHeader = true;
                continue;
            }

            if (fields.Length != 5)
            {
                throw new FraudBlacklistCsvFormatException($"line {lineNumber}: expected 5 columns, found {fields.Length}.");
            }

            var entryTypeText = fields[0].Trim();
            if (!Enum.TryParse<FraudBlacklistEntryType>(entryTypeText, ignoreCase: true, out var entryType))
            {
                throw new FraudBlacklistCsvFormatException($"line {lineNumber}: unknown entry_type '{entryTypeText}'.");
            }

            var value = fields[1].Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
            {
                throw new FraudBlacklistCsvFormatException($"line {lineNumber}: value must be 1-256 characters.");
            }

            var kind = fields[2].Trim();
            if (!string.Equals(kind, "blacklist", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(kind, "whitelist", StringComparison.OrdinalIgnoreCase))
            {
                throw new FraudBlacklistCsvFormatException($"line {lineNumber}: kind must be 'blacklist' or 'whitelist'.");
            }

            var reason = fields[3].Trim();
            if (reason.Length > 500)
            {
                throw new FraudBlacklistCsvFormatException($"line {lineNumber}: reason exceeds 500 characters.");
            }

            DateTime? expiresAt = null;
            var expiresAtText = fields[4].Trim();
            if (expiresAtText.Length > 0)
            {
                if (!DateTime.TryParse(expiresAtText, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                {
                    throw new FraudBlacklistCsvFormatException($"line {lineNumber}: expires_at '{expiresAtText}' is not a valid ISO-8601 instant.");
                }

                expiresAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            if (rows.Count >= MaxImportRows)
            {
                throw new FraudBlacklistCsvFormatException($"import exceeds the {MaxImportRows}-row limit.");
            }

            rows.Add(new Row(
                lineNumber,
                entryType,
                value,
                reason.Length == 0 ? null : reason,
                string.Equals(kind, "whitelist", StringComparison.OrdinalIgnoreCase),
                expiresAt));
        }

        return rows;
    }

    private static bool IsHeader(string[] fields) =>
        fields.Length == 5
        && string.Equals(fields[0].Trim(), "entry_type", StringComparison.OrdinalIgnoreCase)
        && string.Equals(fields[1].Trim(), "value", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Splits CSV text into records of unquoted fields (RFC 4180: quoted fields, "" escapes,
    ///     CRLF/CR/LF), each tagged with the 1-based physical line the record starts on. Blank
    ///     lines consume their line number without producing a record.
    /// </summary>
    private static IEnumerable<(string[] Fields, int LineNumber)> SplitRecords(string csv)
    {
        var record = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;
        var recordStarted = false;
        var line = 1;
        var recordStartLine = 1;

        (string[] Fields, int LineNumber) EndRecord()
        {
            record.Add(field.ToString());
            field.Clear();
            fieldStarted = false;
            recordStarted = false;
            var fields = record.ToArray();
            record.Clear();
            return (fields, recordStartLine);
        }

        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];
            if (inQuotes)
            {
                if (character == '\n')
                {
                    line++;
                }

                if (character == '"')
                {
                    if (index + 1 < csv.Length && csv[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                }
            }
            else if (character == '"' && !fieldStarted)
            {
                inQuotes = true;
                fieldStarted = true;
                if (!recordStarted)
                {
                    recordStarted = true;
                    recordStartLine = line;
                }
            }
            else if (character == ',')
            {
                record.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
                if (!recordStarted)
                {
                    recordStarted = true;
                    recordStartLine = line;
                }
            }
            else if (character == '\r' || character == '\n')
            {
                if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                {
                    index++;
                }

                if (recordStarted)
                {
                    yield return EndRecord();
                }

                line++;
            }
            else
            {
                field.Append(character);
                fieldStarted = true;
                if (!recordStarted)
                {
                    recordStarted = true;
                    recordStartLine = line;
                }
            }
        }

        if (recordStarted)
        {
            yield return EndRecord();
        }
    }

    private static string Field(string value) =>
        value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}

/// <summary>Raised when a blacklist CSV import body is malformed.</summary>
public sealed class FraudBlacklistCsvFormatException(string message) : FormatException(message);
