using Microsoft.EntityFrameworkCore;
using GameGuild;

namespace GameGuild.Assets.Queries;

public sealed record GetAssetStatisticsQuery : IRequest<AssetStatisticsResponse>;

public sealed record AssetStatisticsResponse(
    int TotalAssets,
    int TotalContentObjects,
    long TotalBytes,
    int DocumentAssets,
    int ImageAssets,
    int VideoAssets,
    long TotalAccesses,
    int PendingVirusScans,
    int PendingModeration,
    int BlockedOrRejected,
    int LegalHoldContent,
    int RetentionCandidates);

public sealed class GetAssetStatisticsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssetStatisticsQuery, AssetStatisticsResponse>
{
    public async Task<AssetStatisticsResponse> Handle(GetAssetStatisticsQuery request, CancellationToken ct = default)
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
}
