namespace GameGuild.Assets.Commands;

public sealed record BulkUploadAssetInput(
    Stream Content,
    string FileName,
    string MimeType,
    string? DisplayName = null);

public sealed record BulkUploadAssetsCommand(
    IReadOnlyList<BulkUploadAssetInput> Files,
    Guid UserId,
    Guid? TenantId,
    AssetAccessPolicy AccessPolicy = AssetAccessPolicy.Private,
    string? ParentResourceType = null,
    Guid? ParentResourceId = null) : IRequest<BulkUploadAssetsResponse>;

public sealed record BulkUploadAssetsResponse(
    int TotalRequested,
    int Successful,
    int Failed,
    IReadOnlyList<BulkUploadAssetItem> Items);

public sealed record BulkUploadAssetItem(
    string FileName,
    bool Success,
    Guid? AssetReferenceId,
    Guid? AssetContentId,
    string? Error);

public sealed class BulkUploadAssetsHandler(
    IAssetUploadService uploadService) : IRequestHandler<BulkUploadAssetsCommand, BulkUploadAssetsResponse>
{
    public async Task<BulkUploadAssetsResponse> Handle(
        BulkUploadAssetsCommand request,
        CancellationToken ct = default)
    {
        var items = new List<BulkUploadAssetItem>();

        foreach (var file in request.Files)
        {
            try
            {
                var options = new UploadAssetOptions(
                    file.DisplayName ?? file.FileName,
                    request.AccessPolicy,
                    request.ParentResourceType,
                    request.ParentResourceId);

                var result = await uploadService
                    .UploadAsync(file.Content, file.FileName, file.MimeType, request.UserId, options, ct)
                    .ConfigureAwait(false);

                items.Add(result.Success
                    ? new BulkUploadAssetItem(file.FileName, true, result.AssetReferenceId, result.AssetContentId, null)
                    : new BulkUploadAssetItem(file.FileName, false, null, null, result.Error ?? "Upload failed."));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                items.Add(new BulkUploadAssetItem(file.FileName, false, null, null, ex.Message));
            }
        }

        return new BulkUploadAssetsResponse(
            request.Files.Count,
            items.Count(item => item.Success),
            items.Count(item => !item.Success),
            items);
    }
}
