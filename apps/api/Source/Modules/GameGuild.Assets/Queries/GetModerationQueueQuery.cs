using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Queries;

/// <summary>
/// Query to get pending moderation queue (admin only).
/// </summary>
public record GetModerationQueueQuery(
    int Limit = 100) : IRequest<Result<IReadOnlyList<ReportDto>>>;

public record ReportDto(
    Guid Id,
    Guid AssetReferenceId,
    Guid ReportedByUserId,
    ReportReason Reason,
    string? Description,
    ReportStatus Status,
    ReviewDecision? Decision,
    Guid? ReviewedByUserId,
    string? ReviewNotes,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    AssetDto? Asset);

public class GetModerationQueueValidator : AbstractValidator<GetModerationQueueQuery>
{
    public GetModerationQueueValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 500);
    }
}

public class GetModerationQueueHandler : IRequestHandler<GetModerationQueueQuery, Result<IReadOnlyList<ReportDto>>>
{
    private readonly IAssetModerationService _moderationService;

    public GetModerationQueueHandler(IAssetModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    public async Task<Result<IReadOnlyList<ReportDto>>> HandleAsync(
        GetModerationQueueQuery request,
        CancellationToken ct = default)
    {
        var reports = await _moderationService.GetPendingReportsAsync(request.Limit, ct);

        var result = reports.Select(report =>
        {
            AssetDto? assetDto = null;
            if (report.Reference != null)
            {
                AssetContentDto? contentDto = null;
                if (report.Reference.Content != null)
                {
                    contentDto = new AssetContentDto(
                        report.Reference.Content.Id,
                        report.Reference.Content.ContentHash,
                        report.Reference.Content.MimeType,
                        report.Reference.Content.SizeBytes,
                        report.Reference.Content.Width,
                        report.Reference.Content.Height,
                        report.Reference.Content.VirusScanStatus,
                        report.Reference.Content.ModerationStatus);
                }

                assetDto = new AssetDto(
                    report.Reference.Id,
                    report.Reference.AssetContentId,
                    report.Reference.CreatedByUserId,
                    report.Reference.DisplayName,
                    report.Reference.AccessPolicy,
                    report.Reference.ParentResourceType,
                    report.Reference.ParentResourceId,
                    report.Reference.AccessCount,
                    report.Reference.LastAccessedAt,
                    report.Reference.CreatedAt,
                    report.Reference.UpdatedAt,
                    contentDto);
            }

            return new ReportDto(
                report.Id,
                report.AssetReferenceId,
                report.ReportedByUserId,
                report.Reason,
                report.Description,
                report.Status,
                report.Decision,
                report.ReviewedByUserId,
                report.ReviewNotes,
                report.CreatedAt,
                report.ReviewedAt,
                assetDto);
        }).ToList();

        return Result<IReadOnlyList<ReportDto>>.Success(result);
    }
}
