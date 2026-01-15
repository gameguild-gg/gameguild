using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Queries;

/// <summary>
/// Query to get reports for an asset (admin only).
/// </summary>
public record GetAssetReportsQuery(
    Guid AssetReferenceId) : IRequest<Result<IReadOnlyList<ReportDto>>>;

public class GetAssetReportsValidator : AbstractValidator<GetAssetReportsQuery>
{
    public GetAssetReportsValidator()
    {
        RuleFor(x => x.AssetReferenceId).NotEmpty();
    }
}

public class GetAssetReportsHandler : IRequestHandler<GetAssetReportsQuery, Result<IReadOnlyList<ReportDto>>>
{
    private readonly IAssetReportRepository _reportRepository;

    public GetAssetReportsHandler(IAssetReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<IReadOnlyList<ReportDto>>> HandleAsync(
        GetAssetReportsQuery request,
        CancellationToken ct = default)
    {
        var reports = await _reportRepository.GetByAssetReferenceAsync(request.AssetReferenceId, ct);

        var result = reports.Select(report => new ReportDto(
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
            null)).ToList();

        return Result<IReadOnlyList<ReportDto>>.Success(result);
    }
}
