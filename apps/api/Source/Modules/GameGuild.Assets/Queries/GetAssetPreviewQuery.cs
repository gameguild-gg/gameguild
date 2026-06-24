namespace GameGuild.Assets.Queries;

/// <summary>
/// Query that builds the document preview contract used by inline viewers.
/// </summary>
public sealed record GetAssetPreviewQuery(
    Guid AssetReferenceId,
    Guid? UserId,
    Guid? TenantId,
    int ThumbnailWidth = 320,
    int ThumbnailHeight = 240,
    bool IncludeExtractedText = false,
    int TextPreviewLength = 2000) : IRequest<AssetPreviewResponse?>;

public sealed record AssetPreviewResponse(
    Guid AssetReferenceId,
    Guid AssetContentId,
    string? DisplayName,
    string MimeType,
    AssetKind Kind,
    string PreviewMode,
    bool CanInlinePreview,
    bool IsBlocked,
    string? ContentUrl,
    string? ThumbnailUrl,
    DateTimeOffset? ExpiresAt,
    string? ExtractedTextPreview,
    bool UsedOcr,
    bool IsTextTruncated,
    IReadOnlyList<string> Warnings);

public sealed class GetAssetPreviewHandler(
    IAssetReferenceRepository referenceRepository,
    IAssetAccessService accessService,
    IAssetTextExtractionService textExtractionService) : IRequestHandler<GetAssetPreviewQuery, AssetPreviewResponse?>
{
    public async Task<AssetPreviewResponse?> Handle(GetAssetPreviewQuery request, CancellationToken ct = default)
    {
        var reference = await referenceRepository
            .GetByIdWithContentAsync(request.AssetReferenceId, ct)
            .ConfigureAwait(false);

        if (reference?.Content is null)
        {
            return null;
        }

        var validation = await accessService
            .ValidateAccessAsync(request.AssetReferenceId, request.UserId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (!validation.IsValid)
        {
            return null;
        }

        var blocked = reference.Content.VirusScanStatus == VirusScanStatus.Infected ||
                      reference.Content.ModerationStatus == ModerationStatus.Blocked ||
                      reference.Content.ModerationStatus == ModerationStatus.Rejected;

        if (blocked)
        {
            return new AssetPreviewResponse(
                reference.Id,
                reference.AssetContentId,
                reference.DisplayName,
                reference.Content.MimeType,
                reference.Content.Kind,
                "blocked",
                false,
                true,
                null,
                null,
                null,
                null,
                false,
                false,
                ["Asset is blocked by virus scanning or moderation."]);
        }

        var mode = ResolvePreviewMode(reference.Content);
        var contentUrl = await accessService
            .GenerateAccessUrlAsync(request.AssetReferenceId, request.UserId, request.TenantId, null, ct)
            .ConfigureAwait(false);

        if (contentUrl is null)
        {
            return null;
        }

        var thumbnailUrl = await TryGenerateThumbnailUrlAsync(reference, request, ct).ConfigureAwait(false);
        var extraction = request.IncludeExtractedText || mode == "text"
            ? await textExtractionService.ExtractAsync(reference, ct).ConfigureAwait(false)
            : null;

        var textPreview = extraction is null
            ? null
            : TrimTextPreview(extraction.Text, request.TextPreviewLength);

        var textWasTrimmed = extraction?.IsTruncated == true ||
                             (!string.IsNullOrEmpty(extraction?.Text) &&
                              textPreview is not null &&
                              extraction.Text.Length > textPreview.Length);

        return new AssetPreviewResponse(
            reference.Id,
            reference.AssetContentId,
            reference.DisplayName,
            reference.Content.MimeType,
            reference.Content.Kind,
            mode,
            mode is "image" or "pdf" or "text",
            false,
            contentUrl.Url,
            thumbnailUrl?.Url,
            contentUrl.ExpiresAt,
            textPreview,
            extraction?.UsedOcr ?? false,
            textWasTrimmed,
            extraction?.Warnings ?? []);
    }

    private async Task<AssetAccessUrl?> TryGenerateThumbnailUrlAsync(
        AssetReference reference,
        GetAssetPreviewQuery request,
        CancellationToken ct)
    {
        if (reference.Content.Kind != AssetKind.Image)
        {
            return null;
        }

        var transformation = new TransformationSpec
        {
            Width = Math.Clamp(request.ThumbnailWidth, 1, 2048),
            Height = Math.Clamp(request.ThumbnailHeight, 1, 2048),
            Fit = ImageFit.Cover,
            Format = ImageFormat.Webp,
            Quality = 82
        };

        return await accessService
            .GenerateAccessUrlAsync(request.AssetReferenceId, request.UserId, request.TenantId, transformation, ct)
            .ConfigureAwait(false);
    }

    private static string ResolvePreviewMode(AssetContent content)
    {
        if (content.Kind == AssetKind.Image)
        {
            return "image";
        }

        if (string.Equals(content.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "pdf";
        }

        if (content.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
            content.MimeType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
            content.MimeType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
            content.MimeType.Contains("csv", StringComparison.OrdinalIgnoreCase))
        {
            return "text";
        }

        return "download";
    }

    private static string? TrimTextPreview(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var limit = Math.Clamp(maxLength, 1, 20_000);
        return text.Length <= limit ? text : text[..limit];
    }
}
