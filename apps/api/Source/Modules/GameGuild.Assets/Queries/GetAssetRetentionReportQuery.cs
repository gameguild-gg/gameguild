using Microsoft.EntityFrameworkCore;
using GameGuild;

namespace GameGuild.Assets.Queries;

public sealed record GetAssetRetentionReportQuery(
    int GracePeriodHours = 24,
    int Limit = 100) : IRequest<AssetRetentionReportResponse>;

public sealed record AssetRetentionReportResponse(
    int GracePeriodHours,
    int Limit,
    int Candidates,
    int OnLegalHold,
    int MarkedForDeletion,
    long CandidateBytes,
    IReadOnlyList<AssetRetentionCandidateResponse> Items);

public sealed record AssetRetentionCandidateResponse(
    Guid AssetContentId,
    string BucketName,
    string ObjectKey,
    string MimeType,
    long SizeBytes,
    DateTime? MarkedForDeletionAt);

public sealed class GetAssetRetentionReportHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssetRetentionReportQuery, AssetRetentionReportResponse>
{
    public async Task<AssetRetentionReportResponse> Handle(
        GetAssetRetentionReportQuery request,
        CancellationToken ct = default)
    {
        var graceHours = Math.Clamp(request.GracePeriodHours, 1, 24 * 365);
        var limit = Math.Clamp(request.Limit, 1, 10_000);
        var cutoff = SystemClock.UtcNow - TimeSpan.FromHours(graceHours);
        var contents = db.Set<AssetContent>();

        var candidates = await contents
            .Where(content => content.ReferenceCount == 0)
            .Where(content => content.MarkedForDeletionAt != null && content.MarkedForDeletionAt < cutoff)
            .Where(content => content.IsDeletable)
            .OrderBy(content => content.MarkedForDeletionAt)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var onLegalHold = await contents.CountAsync(content => !content.IsDeletable, ct).ConfigureAwait(false);
        var markedForDeletion = await contents.CountAsync(content => content.MarkedForDeletionAt != null, ct).ConfigureAwait(false);

        return new AssetRetentionReportResponse(
            graceHours,
            limit,
            candidates.Count,
            onLegalHold,
            markedForDeletion,
            candidates.Sum(content => content.SizeBytes),
            candidates.Select(content => new AssetRetentionCandidateResponse(
                content.Id,
                content.BucketName,
                content.ObjectKey,
                content.MimeType,
                content.SizeBytes,
                content.MarkedForDeletionAt)).ToList());
    }
}
