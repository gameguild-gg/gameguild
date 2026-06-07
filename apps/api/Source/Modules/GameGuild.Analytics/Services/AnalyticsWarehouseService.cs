using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Analytics;

public interface IAnalyticsDataWarehouseService
{
    Task<AnalyticsWarehouseRunResponse> MaterializeAsync(
        AnalyticsWarehouseRunRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<AnalyticsWarehouseFactDto>> GetFactsAsync(
        AnalyticsWarehouseExportRequest request,
        CancellationToken ct = default);

    string BuildCsv(IReadOnlyList<AnalyticsWarehouseFactDto> facts);
}

public sealed class AnalyticsDataWarehouseService(
    IApplicationDbContext db,
    IOptions<AnalyticsWarehouseOptions> options) : IAnalyticsDataWarehouseService
{
    public static readonly ActivitySource ActivitySource = new("GameGuild.Analytics.Warehouse", "1.0.0");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AnalyticsWarehouseRunResponse> MaterializeAsync(
        AnalyticsWarehouseRunRequest request,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("analytics.warehouse.materialize", ActivityKind.Internal);

        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            throw new InvalidOperationException("Analytics warehouse materialization is disabled.");
        }

        var asOfUtc = NormalizeUtc(request.AsOfUtc) ?? DateTime.UtcNow;
        var lookbackDays = Math.Clamp(request.LookbackDays ?? currentOptions.DefaultLookbackDays, 1, 366);
        var startUtc = asOfUtc.Date.AddDays(-lookbackDays + 1);
        var tenantId = request.TenantId;
        var runId = Guid.NewGuid();
        var events = new List<AnalyticsEvent>();

        await db.Set<AnalyticsEvent>().AddRangeAsync(events, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        activity?.SetTag("warehouse.run_id", runId);
        activity?.SetTag("warehouse.fact_count", events.Count);
        activity?.SetTag("tenant.id", tenantId);

        return new AnalyticsWarehouseRunResponse(
            runId,
            tenantId,
            startUtc,
            asOfUtc,
            events.Count,
            events.GroupBy(current => current.EventName)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<AnalyticsWarehouseFactDto>> GetFactsAsync(
        AnalyticsWarehouseExportRequest request,
        CancellationToken ct = default)
    {
        var startUtc = NormalizeUtc(request.StartUtc) ?? DateTime.UtcNow.Date.AddDays(-30);
        var endUtc = NormalizeUtc(request.EndUtc) ?? DateTime.UtcNow;
        var take = Math.Clamp(request.Take ?? 1000, 1, 5000);

        var query = db.Set<AnalyticsEvent>()
            .AsNoTracking()
            .Where(current => current.DeletedAt == null)
            .Where(current => current.EventName.StartsWith("warehouse."))
            .Where(current => current.Timestamp >= startUtc && current.Timestamp <= endUtc);

        if (request.TenantId.HasValue)
        {
            query = query.Where(current => current.TenantId == request.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.FactName))
        {
            query = query.Where(current => current.EventName == request.FactName.Trim());
        }

        var events = await query
            .OrderByDescending(current => current.Timestamp)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return events.Select(ToFactDto).ToList();
    }

    public string BuildCsv(IReadOnlyList<AnalyticsWarehouseFactDto> facts)
    {
        var csv = new StringBuilder();
        csv.AppendLine("id,tenantId,factName,timestamp,metric,count,amountUsd,dimensions");

        foreach (var fact in facts)
        {
            csv.Append(fact.Id).Append(',')
                .Append(fact.TenantId).Append(',')
                .Append(EscapeCsv(fact.FactName)).Append(',')
                .Append(fact.Timestamp.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                .Append(EscapeCsv(fact.Metric)).Append(',')
                .Append(fact.Count?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                .Append(fact.AmountUsd?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                .Append(EscapeCsv(JsonSerializer.Serialize(fact.Dimensions, JsonOptions)))
                .AppendLine();
        }

        return csv.ToString();
    }

    private static AnalyticsEvent BuildFact(
        string factName,
        Guid runId,
        Guid? tenantId,
        DateTime timestamp,
        string metric,
        int count,
        decimal amountUsd,
        IReadOnlyDictionary<string, string?> dimensions)
    {
        var payload = new AnalyticsWarehousePayload(runId, metric, count, amountUsd, dimensions);
        return new AnalyticsEvent
        {
            EventName = factName,
            TenantId = tenantId,
            Timestamp = timestamp,
            Environment = "warehouse",
            Properties = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }

    private static AnalyticsWarehouseFactDto ToFactDto(AnalyticsEvent analyticsEvent)
    {
        AnalyticsWarehousePayload? payload = null;
        if (!string.IsNullOrWhiteSpace(analyticsEvent.Properties))
        {
            try
            {
                payload = JsonSerializer.Deserialize<AnalyticsWarehousePayload>(analyticsEvent.Properties, JsonOptions);
            }
            catch (JsonException)
            {
                payload = null;
            }
        }

        return new AnalyticsWarehouseFactDto(
            analyticsEvent.Id,
            analyticsEvent.TenantId,
            analyticsEvent.EventName,
            analyticsEvent.Timestamp,
            payload?.RunId,
            payload?.Metric ?? string.Empty,
            payload?.Count,
            payload?.AmountUsd,
            payload?.Dimensions ?? new Dictionary<string, string?>());
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : value.Value.ToUniversalTime();
    }

    private static string EscapeCsv(string value)
        => value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}

public sealed class AnalyticsWarehouseOptions
{
    public const string SectionName = "Analytics:Warehouse";

    public bool Enabled { get; set; } = true;
    public int DefaultLookbackDays { get; set; } = 30;
}

public sealed record AnalyticsWarehouseRunRequest(
    DateTime? AsOfUtc = null,
    int? LookbackDays = null,
    Guid? TenantId = null);

public sealed record AnalyticsWarehouseRunResponse(
    Guid RunId,
    Guid? TenantId,
    DateTime StartUtc,
    DateTime AsOfUtc,
    int FactsCreated,
    IReadOnlyDictionary<string, int> FactsByName);

public sealed record AnalyticsWarehouseExportRequest(
    DateTime? StartUtc = null,
    DateTime? EndUtc = null,
    Guid? TenantId = null,
    string? FactName = null,
    int? Take = null);

public sealed record AnalyticsWarehouseFactDto(
    Guid Id,
    Guid? TenantId,
    string FactName,
    DateTime Timestamp,
    Guid? RunId,
    string Metric,
    int? Count,
    decimal? AmountUsd,
    IReadOnlyDictionary<string, string?> Dimensions);

internal sealed record AnalyticsWarehousePayload(
    Guid RunId,
    string Metric,
    int Count,
    decimal AmountUsd,
    IReadOnlyDictionary<string, string?> Dimensions);
