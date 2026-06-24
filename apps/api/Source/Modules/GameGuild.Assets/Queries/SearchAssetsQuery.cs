using Microsoft.EntityFrameworkCore;
using GameGuild;

namespace GameGuild.Assets.Queries;

public sealed record SearchAssetsQuery(
    string? Query,
    Guid? UserId,
    Guid? TenantId,
    AssetKind? Kind = null,
    string? ParentResourceType = null,
    Guid? ParentResourceId = null,
    int Skip = 0,
    int Take = 50) : IRequest<AssetSearchResponse>;

public sealed record AssetSearchResponse(
    int TotalMatched,
    int Returned,
    IReadOnlyList<AssetSearchResult> Items);

public sealed record AssetSearchResult(
    Guid AssetReferenceId,
    Guid AssetContentId,
    string? DisplayName,
    string? OriginalFilename,
    string? ParentResourceType,
    Guid? ParentResourceId,
    string MimeType,
    AssetKind Kind,
    long SizeBytes,
    long AccessCount,
    DateTime CreatedAt,
    DateTime? LastAccessedAt);

public sealed class SearchAssetsHandler(
    IApplicationDbContext db,
    IAssetAccessService accessService) : IRequestHandler<SearchAssetsQuery, AssetSearchResponse>
{
    public async Task<AssetSearchResponse> Handle(SearchAssetsQuery request, CancellationToken ct = default)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 200);
        var term = request.Query?.Trim();

        var query = db.Set<AssetReference>()
            .Include(reference => reference.Content)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            query = query.Where(reference =>
                EF.Functions.Like(reference.DisplayName ?? string.Empty, pattern) ||
                EF.Functions.Like(reference.OriginalFilename ?? string.Empty, pattern) ||
                EF.Functions.Like(reference.Description ?? string.Empty, pattern) ||
                EF.Functions.Like(reference.Tags ?? string.Empty, pattern) ||
                EF.Functions.Like(reference.Content.MimeType, pattern) ||
                EF.Functions.Like(reference.Content.ObjectKey, pattern));
        }

        if (request.Kind.HasValue)
        {
            query = query.Where(reference => reference.Content.Kind == request.Kind.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ParentResourceType))
        {
            query = query.Where(reference => reference.ParentResourceType == request.ParentResourceType);
        }

        if (request.ParentResourceId.HasValue)
        {
            query = query.Where(reference => reference.ParentResourceId == request.ParentResourceId.Value);
        }

        var candidates = await query
            .OrderByDescending(reference => reference.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var visible = new List<AssetSearchResult>();
        foreach (var reference in candidates)
        {
            var validation = await accessService
                .ValidateAccessAsync(reference.Id, request.UserId, request.TenantId, ct)
                .ConfigureAwait(false);

            if (!validation.IsValid)
            {
                continue;
            }

            visible.Add(new AssetSearchResult(
                reference.Id,
                reference.AssetContentId,
                reference.DisplayName,
                reference.OriginalFilename,
                reference.ParentResourceType,
                reference.ParentResourceId,
                reference.Content.MimeType,
                reference.Content.Kind,
                reference.Content.SizeBytes,
                reference.AccessCount,
                reference.CreatedAt,
                reference.LastAccessedAt));
        }

        return new AssetSearchResponse(candidates.Count, visible.Count, visible);
    }
}
