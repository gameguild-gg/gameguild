using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for exporting analytics data
/// </summary>
public sealed class ExportAnalyticsQueryHandler(IFeatureFlagAnalyticsRepository analyticsRepository) : IQueryHandler<ExportAnalyticsQuery, AnalyticsExportResult>
{
    private readonly IFeatureFlagAnalyticsRepository _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));

    public async Task<AnalyticsExportResult> Handle(ExportAnalyticsQuery request, CancellationToken cancellationToken)
    {
        // Export analytics data based on the query parameters
        var exportResult = await _analyticsRepository.ExportAnalyticsAsync(
            request.FeatureKeys,
            request.StartDate,
            request.EndDate,
            request.Format,
            request.IncludeDetails,
            request.GroupBy,
            request.Environment,
            request.TenantId,
            cancellationToken
        ).ConfigureAwait(false);

        return exportResult;
    }
}
