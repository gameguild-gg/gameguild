using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace GameGuild.Compliance.Audit;

public enum AuditExportProgressStatus
{
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public sealed record AuditExportProgressResponse(
    Guid ExportId,
    string Status,
    int TotalRecords,
    int RecordsWritten,
    double PercentComplete,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    string? ErrorMessage);

public interface IAuditExportProgressTracker
{
    Task BeginAsync(Guid exportId, Guid ownerUserId, int totalRecords, CancellationToken cancellationToken);

    Task ReportAsync(
        Guid exportId,
        Guid ownerUserId,
        int recordsWritten,
        AuditExportProgressStatus status,
        string? errorMessage,
        CancellationToken cancellationToken);

    Task<AuditExportProgressResponse?> GetAsync(Guid exportId, Guid ownerUserId, CancellationToken cancellationToken);
}

public sealed class DistributedAuditExportProgressTracker(IDistributedCache cache) : IAuditExportProgressTracker
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task BeginAsync(Guid exportId, Guid ownerUserId, int totalRecords, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var progress = new StoredAuditExportProgress(
            exportId,
            ownerUserId,
            AuditExportProgressStatus.InProgress,
            Math.Max(totalRecords, 0),
            0,
            now,
            now,
            null);

        return StoreAsync(progress, cancellationToken);
    }

    public async Task ReportAsync(
        Guid exportId,
        Guid ownerUserId,
        int recordsWritten,
        AuditExportProgressStatus status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var progress = await ReadAsync(exportId, cancellationToken).ConfigureAwait(false);
        if (progress is null || progress.OwnerUserId != ownerUserId)
        {
            throw new InvalidOperationException("Audit export progress is unavailable for this requester.");
        }

        var updated = progress with
        {
            Status = status,
            RecordsWritten = Math.Clamp(recordsWritten, progress.RecordsWritten, progress.TotalRecords),
            UpdatedAt = DateTimeOffset.UtcNow,
            ErrorMessage = errorMessage
        };

        await StoreAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuditExportProgressResponse?> GetAsync(
        Guid exportId,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var progress = await ReadAsync(exportId, cancellationToken).ConfigureAwait(false);
        if (progress is null || progress.OwnerUserId != ownerUserId) { return null; }

        var percentComplete = progress.TotalRecords == 0
            ? progress.Status == AuditExportProgressStatus.InProgress ? 0 : 100
            : Math.Round(100d * progress.RecordsWritten / progress.TotalRecords, 1);

        return new AuditExportProgressResponse(
            progress.ExportId,
            progress.Status.ToString(),
            progress.TotalRecords,
            progress.RecordsWritten,
            percentComplete,
            progress.StartedAt,
            progress.UpdatedAt,
            progress.ErrorMessage);
    }

    private async Task<StoredAuditExportProgress?> ReadAsync(Guid exportId, CancellationToken cancellationToken)
    {
        var serialized = await cache.GetStringAsync(GetKey(exportId), cancellationToken).ConfigureAwait(false);
        return serialized is null
            ? null
            : JsonSerializer.Deserialize<StoredAuditExportProgress>(serialized, JsonOptions);
    }

    private Task StoreAsync(StoredAuditExportProgress progress, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(progress, JsonOptions);
        return cache.SetStringAsync(GetKey(progress.ExportId), serialized, CacheOptions, cancellationToken);
    }

    private static string GetKey(Guid exportId) => $"compliance:audit:export-progress:{exportId:N}";

    private sealed record StoredAuditExportProgress(
        Guid ExportId,
        Guid OwnerUserId,
        AuditExportProgressStatus Status,
        int TotalRecords,
        int RecordsWritten,
        DateTimeOffset StartedAt,
        DateTimeOffset UpdatedAt,
        string? ErrorMessage);
}
