using Microsoft.Extensions.Logging;
using System.Text;

namespace GameGuild.Assets;

/// <summary>
/// Implementation of content moderation service.
/// </summary>
public class AssetModerationService : IAssetModerationService
{
    private readonly IAssetContentRepository _contentRepository;
    private readonly IAssetReportRepository _reportRepository;
    private readonly ILogger<AssetModerationService> _logger;

    public AssetModerationService(
        IAssetContentRepository contentRepository,
        IAssetReportRepository reportRepository,
        ILogger<AssetModerationService> logger)
    {
        _contentRepository = contentRepository;
        _reportRepository = reportRepository;
        _logger = logger;
    }

    public async Task<ModerationResult> ModerateAsync(
        Guid assetContentId,
        Stream content,
        string mimeType,
        CancellationToken ct = default)
    {
        var assetContent = await _contentRepository.GetByIdAsync(assetContentId, ct).ConfigureAwait(false);
        if (assetContent == null)
        {
            return new ModerationResult(false, ModerationStatus.Pending, 0, null, "Asset not found");
        }

        var localPolicy = await EvaluateLocalPolicyAsync(content, mimeType, ct).ConfigureAwait(false);

        // Update content status
        assetContent.SetModerationStatus(
            localPolicy.Status,
            localPolicy.Issue is null ? null : [localPolicy.Issue]);
        await _contentRepository.UpdateAsync(assetContent, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Moderated content {ContentId}: Status={Status}, Confidence={Confidence}",
            assetContentId, localPolicy.Status, localPolicy.Confidence);

        return new ModerationResult(
            localPolicy.Status is ModerationStatus.Approved or ModerationStatus.ApprovedWithWarning,
            localPolicy.Status,
            localPolicy.Confidence,
            localPolicy.Issue,
            null);
    }

    public async Task<IReadOnlyList<AssetReport>> GetPendingReportsAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        return await _reportRepository.GetPendingReportsAsync(limit, ct).ConfigureAwait(false);
    }

    public async Task<bool> SubmitReviewAsync(
        Guid reportId,
        Guid reviewerId,
        ReviewDecision decision,
        string? notes = null,
        CancellationToken ct = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, ct).ConfigureAwait(false);
        if (report == null)
        {
            return false;
        }

        report.SubmitReview(reviewerId, decision, notes);
        await _reportRepository.UpdateAsync(report, ct).ConfigureAwait(false);

        // If blocked, update the content status
        if (decision == ReviewDecision.BlockContent && report.Reference?.Content != null)
        {
            report.Reference.Content.SetModerationStatus(ModerationStatus.Blocked);
            await _contentRepository.UpdateAsync(report.Reference.Content, ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Report {ReportId} reviewed by {ReviewerId}: Decision={Decision}",
            reportId, reviewerId, decision);

        return true;
    }

    public async Task<AssetReport?> CreateReportAsync(
        Guid assetReferenceId,
        Guid reportedByUserId,
        ReportReason reason,
        string? description = null,
        CancellationToken ct = default)
    {
        // Check if user already reported this asset
        if (await _reportRepository.HasUserReportedAsync(assetReferenceId, reportedByUserId, ct))
        {
            _logger.LogWarning(
                "User {UserId} has already reported asset {AssetId}",
                reportedByUserId, assetReferenceId);
            return null;
        }

        var report = new AssetReport(assetReferenceId, reportedByUserId, reason, description);
        return await _reportRepository.AddAsync(report, ct).ConfigureAwait(false);
    }

    private static async Task<(ModerationStatus Status, double Confidence, string? Issue)> EvaluateLocalPolicyAsync(
        Stream content,
        string mimeType,
        CancellationToken ct)
    {
        var sample = await ReadSampleAsync(content, 4096, ct).ConfigureAwait(false);
        if (sample.Length == 0)
        {
            return (ModerationStatus.Rejected, 1.0, "Content is empty");
        }

        if (HasExecutableSignature(sample))
        {
            return (ModerationStatus.Blocked, 1.0, "Executable binary signature detected");
        }

        var normalizedMime = mimeType.Trim().ToLowerInvariant();
        if (normalizedMime.StartsWith("image/", StringComparison.Ordinal))
        {
            return HasExpectedImageSignature(normalizedMime, sample)
                ? (ModerationStatus.Approved, 0.99, null)
                : (ModerationStatus.NeedsReview, 0.82, "Image header did not match declared MIME type");
        }

        if (normalizedMime == "application/pdf")
        {
            return StartsWithAscii(sample, "%PDF")
                ? (ModerationStatus.Approved, 0.98, null)
                : (ModerationStatus.NeedsReview, 0.84, "PDF header did not match declared MIME type");
        }

        if (normalizedMime.StartsWith("text/", StringComparison.Ordinal) ||
            normalizedMime.Contains("json", StringComparison.Ordinal) ||
            normalizedMime.Contains("xml", StringComparison.Ordinal))
        {
            var text = Encoding.UTF8.GetString(sample.Span).ToLowerInvariant();
            var marker = GetBlockedTextMarkers().FirstOrDefault(text.Contains);
            return marker is null
                ? (ModerationStatus.Approved, 0.96, null)
                : (ModerationStatus.NeedsReview, 0.88, $"Text content matched policy marker: {marker}");
        }

        if (normalizedMime.Contains("zip", StringComparison.Ordinal) ||
            normalizedMime.Contains("rar", StringComparison.Ordinal) ||
            normalizedMime.Contains("7z", StringComparison.Ordinal) ||
            normalizedMime == "application/octet-stream")
        {
            return (ModerationStatus.NeedsReview, 0.75, "Binary or archive content requires human review");
        }

        return (ModerationStatus.Approved, 0.99, null);
    }

    private static IReadOnlyList<string> GetBlockedTextMarkers() =>
    [
        "malware",
        "phishing",
        "credential theft",
        "hate speech",
        "explicit threat"
    ];

    private static async Task<ReadOnlyMemory<byte>> ReadSampleAsync(Stream content, int maxBytes, CancellationToken ct)
    {
        var originalPosition = content.CanSeek ? content.Position : (long?)null;
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var buffer = new byte[maxBytes];
        var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);

        if (originalPosition.HasValue)
        {
            content.Position = originalPosition.Value;
        }

        return buffer.AsMemory(0, read);
    }

    private static bool HasExecutableSignature(ReadOnlyMemory<byte> sample)
    {
        var bytes = sample.Span;
        return bytes.Length >= 2 && bytes[0] == 'M' && bytes[1] == 'Z' ||
               bytes.Length >= 4 && bytes[0] == 0x7F && bytes[1] == (byte)'E' && bytes[2] == (byte)'L' && bytes[3] == (byte)'F';
    }

    private static bool HasExpectedImageSignature(string mimeType, ReadOnlyMemory<byte> sample)
    {
        return mimeType switch
        {
            "image/png" => StartsWithBytes(sample, [0x89, (byte)'P', (byte)'N', (byte)'G']),
            "image/jpeg" or "image/jpg" => StartsWithBytes(sample, [0xFF, 0xD8, 0xFF]),
            "image/gif" => StartsWithAscii(sample, "GIF87a") || StartsWithAscii(sample, "GIF89a"),
            "image/webp" => sample.Length >= 12 &&
                            StartsWithAscii(sample, "RIFF") &&
                            sample.Span.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => true
        };
    }

    private static bool StartsWithAscii(ReadOnlyMemory<byte> sample, string value)
        => sample.Span.StartsWith(Encoding.ASCII.GetBytes(value));

    private static bool StartsWithBytes(ReadOnlyMemory<byte> sample, ReadOnlySpan<byte> value)
        => sample.Span.StartsWith(value);
}
