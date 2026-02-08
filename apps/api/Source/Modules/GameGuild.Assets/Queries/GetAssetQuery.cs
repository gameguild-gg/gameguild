using FluentValidation;

namespace GameGuild.Assets.Queries;

/// <summary>
/// Query to get an asset by ID.
/// </summary>
public record GetAssetQuery(
    Guid AssetReferenceId,
    Guid? UserId,
    Guid? TenantId,
    bool IncludeContentDetails = false) : IRequest<AssetDto?>;

public record AssetDto(
    Guid Id,
    Guid AssetContentId,
    Guid CreatedByUserId,
    string? DisplayName,
    AssetAccessPolicy AccessPolicy,
    string? ParentResourceType,
    Guid? ParentResourceId,
    long AccessCount,
    DateTime? LastAccessedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    AssetContentDto? Content);

public record AssetContentDto(
    Guid Id,
    string ContentHash,
    string MimeType,
    long SizeBytes,
    int? Width,
    int? Height,
    VirusScanStatus VirusScanStatus,
    ModerationStatus ModerationStatus);

public class GetAssetValidator : AbstractValidator<GetAssetQuery>
{
    public GetAssetValidator()
    {
        RuleFor(x => x.AssetReferenceId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class GetAssetHandler : IRequestHandler<GetAssetQuery, AssetDto?>
{
    private readonly IAssetReferenceRepository _referenceRepository;
    private readonly IAssetAccessService _accessService;

    public GetAssetHandler(
        IAssetReferenceRepository referenceRepository,
        IAssetAccessService accessService)
    {
        _referenceRepository = referenceRepository;
        _accessService = accessService;
    }

    public async Task<AssetDto?> Handle(
        GetAssetQuery request,
        CancellationToken ct = default)
    {
        var reference = request.IncludeContentDetails
            ? await _referenceRepository.GetByIdWithContentAsync(request.AssetReferenceId, ct)
            : await _referenceRepository.GetByIdAsync(request.AssetReferenceId, ct).ConfigureAwait(false);

        if (reference == null)
        {
            return null;
        }

        // Check access
        var validation = await _accessService.ValidateAccessAsync(
            request.AssetReferenceId,
            request.UserId,
            request.TenantId,
            ct).ConfigureAwait(false);

        if (!validation.IsValid)
        {
            return null;
        }

        AssetContentDto? contentDto = null;
        if (reference.Content != null)
        {
            contentDto = new AssetContentDto(
                reference.Content.Id,
                reference.Content.ContentHash,
                reference.Content.MimeType,
                reference.Content.SizeBytes,
                reference.Content.Width,
                reference.Content.Height,
                reference.Content.VirusScanStatus,
                reference.Content.ModerationStatus);
        }

        return new AssetDto(
            reference.Id,
            reference.AssetContentId,
            reference.CreatedByUserId,
            reference.DisplayName,
            reference.AccessPolicy,
            reference.ParentResourceType,
            reference.ParentResourceId,
            reference.AccessCount,
            reference.LastAccessedAt,
            reference.CreatedAt,
            reference.UpdatedAt,
            contentDto);
    }
}
