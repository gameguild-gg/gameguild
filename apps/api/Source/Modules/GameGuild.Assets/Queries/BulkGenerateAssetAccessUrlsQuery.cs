namespace GameGuild.Assets.Queries;

public sealed record BulkGenerateAssetAccessUrlsQuery(
    IReadOnlyList<Guid> AssetReferenceIds,
    Guid? UserId,
    Guid? TenantId,
    bool DirectStorageUrl = false) : IRequest<BulkAssetAccessUrlsResponse>;

public sealed record BulkAssetAccessUrlsResponse(
    int TotalRequested,
    int Successful,
    int Failed,
    IReadOnlyList<BulkAssetAccessUrlItem> Items);

public sealed record BulkAssetAccessUrlItem(
    Guid AssetReferenceId,
    bool Success,
    string? Url,
    string? Token,
    DateTimeOffset? ExpiresAt,
    string? MimeType,
    string? Error);

public sealed class BulkGenerateAssetAccessUrlsHandler(IAssetAccessService accessService)
    : IRequestHandler<BulkGenerateAssetAccessUrlsQuery, BulkAssetAccessUrlsResponse>
{
    public async Task<BulkAssetAccessUrlsResponse> Handle(
        BulkGenerateAssetAccessUrlsQuery request,
        CancellationToken ct = default)
    {
        var items = new List<BulkAssetAccessUrlItem>();

        foreach (var assetId in request.AssetReferenceIds.Distinct())
        {
            var accessUrl = request.DirectStorageUrl
                ? await accessService
                    .GenerateDirectStorageUrlAsync(assetId, request.UserId, request.TenantId, ct)
                    .ConfigureAwait(false)
                : await accessService
                    .GenerateAccessUrlAsync(assetId, request.UserId, request.TenantId, null, ct)
                    .ConfigureAwait(false);

            items.Add(accessUrl is null
                ? new BulkAssetAccessUrlItem(assetId, false, null, null, null, null, "Access denied or asset not found.")
                : new BulkAssetAccessUrlItem(
                    assetId,
                    true,
                    accessUrl.Url,
                    string.IsNullOrWhiteSpace(accessUrl.Token) ? null : accessUrl.Token,
                    accessUrl.ExpiresAt,
                    accessUrl.MimeType,
                    null));
        }

        return new BulkAssetAccessUrlsResponse(
            request.AssetReferenceIds.Count,
            items.Count(item => item.Success),
            items.Count(item => !item.Success),
            items);
    }
}
