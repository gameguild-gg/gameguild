using System.Text;
using Microsoft.EntityFrameworkCore;
using GameGuild;

namespace GameGuild.Assets.Queries;

public sealed record ExportAssetStatisticsQuery(
    string Format = "csv") : IRequest<AssetStatisticsExportResponse>;

public sealed record AssetStatisticsExportResponse(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed class ExportAssetStatisticsHandler(IApplicationDbContext db)
    : IRequestHandler<ExportAssetStatisticsQuery, AssetStatisticsExportResponse>
{
    public async Task<AssetStatisticsExportResponse> Handle(
        ExportAssetStatisticsQuery request,
        CancellationToken ct = default)
    {
        var stats = await BuildStatisticsAsync(ct).ConfigureAwait(false);
        var generatedAt = SystemClock.UtcNow;
        var format = request.Format.Trim().ToLowerInvariant();

        return format switch
        {
            "pdf" => new AssetStatisticsExportResponse(
                $"asset-statistics-{generatedAt:yyyyMMddHHmmss}.pdf",
                "application/pdf",
                BuildPdf(stats, generatedAt)),
            _ => new AssetStatisticsExportResponse(
                $"asset-statistics-{generatedAt:yyyyMMddHHmmss}.csv",
                "text/csv",
                BuildCsv(stats, generatedAt))
        };
    }

    private async Task<AssetStatisticsResponse> BuildStatisticsAsync(CancellationToken ct)
    {
        var references = db.Set<AssetReference>();
        var contents = db.Set<AssetContent>();
        var retentionCutoff = SystemClock.UtcNow - TimeSpan.FromHours(24);

        return new AssetStatisticsResponse(
            await references.CountAsync(ct).ConfigureAwait(false),
            await contents.CountAsync(ct).ConfigureAwait(false),
            await contents.SumAsync(content => (long?)content.SizeBytes, ct).ConfigureAwait(false) ?? 0,
            await contents.CountAsync(content => content.Kind == AssetKind.Document, ct).ConfigureAwait(false),
            await contents.CountAsync(content => content.Kind == AssetKind.Image, ct).ConfigureAwait(false),
            await contents.CountAsync(content => content.Kind == AssetKind.Video, ct).ConfigureAwait(false),
            await references.SumAsync(reference => (long?)reference.AccessCount, ct).ConfigureAwait(false) ?? 0,
            await contents.CountAsync(content => content.VirusScanStatus == VirusScanStatus.Pending ||
                                                 content.VirusScanStatus == VirusScanStatus.Scanning, ct).ConfigureAwait(false),
            await contents.CountAsync(content => content.ModerationStatus == ModerationStatus.Pending ||
                                                 content.ModerationStatus == ModerationStatus.Processing ||
                                                 content.ModerationStatus == ModerationStatus.NeedsReview, ct).ConfigureAwait(false),
            await contents.CountAsync(content => content.ModerationStatus == ModerationStatus.Blocked ||
                                                 content.ModerationStatus == ModerationStatus.Rejected ||
                                                 content.VirusScanStatus == VirusScanStatus.Infected, ct).ConfigureAwait(false),
            await contents.CountAsync(content => !content.IsDeletable, ct).ConfigureAwait(false),
            await contents.CountAsync(content => content.ReferenceCount == 0 &&
                                                 content.MarkedForDeletionAt != null &&
                                                 content.MarkedForDeletionAt < retentionCutoff &&
                                                 content.IsDeletable, ct).ConfigureAwait(false));
    }

    private static byte[] BuildCsv(AssetStatisticsResponse stats, DateTime generatedAt)
    {
        var builder = new StringBuilder();
        builder.AppendLine("metric,value,generated_at_utc");
        foreach (var (metric, value) in ToRows(stats))
        {
            builder.Append(metric).Append(',').Append(value).Append(',').AppendLine(generatedAt.ToString("O"));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static byte[] BuildPdf(AssetStatisticsResponse stats, DateTime generatedAt)
    {
        var lines = new List<string>
        {
            "GameGuild document statistics",
            $"Generated at UTC: {generatedAt:O}",
            string.Empty
        };
        lines.AddRange(ToRows(stats).Select(row => $"{row.Metric}: {row.Value}"));
        var text = string.Join("\\n", lines).Replace("(", "\\(").Replace(")", "\\)");
        var stream = $"BT /F1 10 Tf 50 760 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream"
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Length + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer << /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\n");
        builder.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static IReadOnlyList<(string Metric, long Value)> ToRows(AssetStatisticsResponse stats)
        =>
        [
            ("total_assets", stats.TotalAssets),
            ("total_content_objects", stats.TotalContentObjects),
            ("total_bytes", stats.TotalBytes),
            ("document_assets", stats.DocumentAssets),
            ("image_assets", stats.ImageAssets),
            ("video_assets", stats.VideoAssets),
            ("total_accesses", stats.TotalAccesses),
            ("pending_virus_scans", stats.PendingVirusScans),
            ("pending_moderation", stats.PendingModeration),
            ("blocked_or_rejected", stats.BlockedOrRejected),
            ("legal_hold_content", stats.LegalHoldContent),
            ("retention_candidates", stats.RetentionCandidates)
        ];
}
