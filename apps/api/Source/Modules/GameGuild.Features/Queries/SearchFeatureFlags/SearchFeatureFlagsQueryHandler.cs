

namespace GameGuild.Features;

/// <summary>
///     Handler for searching feature flags
/// </summary>
public sealed class SearchFeatureFlagsQueryHandler(IFeatureFlagQueryRepository repository) : CQRS.IQueryHandler<SearchFeatureFlagsQuery, PagedResult<FeatureFlagDto>>
{
    public async Task<PagedResult<FeatureFlagDto>> Handle(SearchFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        // Get all feature flags
        var allFlags = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        // Apply search filters
        var filteredFlags = allFlags.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            filteredFlags = filteredFlags.Where(ff => ff.Key.ToLower().Contains(searchLower) || ff.Name.ToLower().Contains(searchLower) || ff.Description.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(request.Environment)) { filteredFlags = filteredFlags.Where(ff => ff.Environment == request.Environment); }

        if (request.IsEnabled.HasValue) { filteredFlags = filteredFlags.Where(ff => ff.IsEnabled == request.IsEnabled.Value); }

        if (request.IsGlobal.HasValue) { filteredFlags = filteredFlags.Where(ff => ff.IsGlobal == request.IsGlobal.Value); }

        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse(request.Type, true, out FeatureFlagType typeFilter)) { filteredFlags = filteredFlags.Where(ff => ff.Type == typeFilter); }

        // Convert to list to materialize the query
        var materializedFlags = filteredFlags.ToList();

        // Calculate pagination
        var totalCount = materializedFlags.Count;
        var skip = (request.Page - 1) * request.PageSize;

        // Apply pagination and map to DTOs
        var items = materializedFlags.Skip(skip).Take(request.PageSize).Select(EntityModelMapper.ToDto).ToList();

        return PagedResult<FeatureFlagDto>.FromPage(items, totalCount, request.Page, request.PageSize);
    }
}
