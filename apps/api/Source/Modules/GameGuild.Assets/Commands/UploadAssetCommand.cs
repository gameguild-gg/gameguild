using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Commands;

/// <summary>
/// Command to upload a new asset.
/// </summary>
public record UploadAssetCommand(
    Stream Content,
    string FileName,
    string MimeType,
    Guid UserId,
    Guid TenantId,
    string? DisplayName = null,
    AssetAccessPolicy AccessPolicy = AssetAccessPolicy.Private,
    string? ParentResourceType = null,
    Guid? ParentResourceId = null) : IRequest<Result<UploadAssetResponse>>;

public record UploadAssetResponse(
    Guid AssetReferenceId,
    Guid AssetContentId,
    string ContentHash,
    bool WasDeduped);

public class UploadAssetValidator : AbstractValidator<UploadAssetCommand>
{
    public UploadAssetValidator()
    {
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.DisplayName).MaximumLength(255).When(x => x.DisplayName != null);
        RuleFor(x => x.ParentResourceType).MaximumLength(100).When(x => x.ParentResourceType != null);
    }
}

public class UploadAssetHandler : IRequestHandler<UploadAssetCommand, Result<UploadAssetResponse>>
{
    private readonly IAssetUploadService _uploadService;
    private readonly IAssetContentRepository _contentRepository;

    public UploadAssetHandler(
        IAssetUploadService uploadService,
        IAssetContentRepository contentRepository)
    {
        _uploadService = uploadService;
        _contentRepository = contentRepository;
    }

    public async Task<Result<UploadAssetResponse>> HandleAsync(
        UploadAssetCommand request,
        CancellationToken ct = default)
    {
        var options = new UploadAssetOptions(
            request.DisplayName ?? request.FileName,
            request.AccessPolicy,
            request.ParentResourceType,
            request.ParentResourceId);

        var result = await _uploadService.UploadAsync(
            request.Content,
            request.FileName,
            request.MimeType,
            request.UserId,
            options,
            ct);

        if (!result.Success)
        {
            return Result<UploadAssetResponse>.Failure(result.Error ?? "Upload failed");
        }

        // Get content to check if it was deduped
        var content = await _contentRepository.GetByIdAsync(result.AssetContentId!.Value, ct);
        var wasDeduped = content?.ReferenceCount > 1;

        return Result<UploadAssetResponse>.Success(new UploadAssetResponse(
            result.AssetReferenceId!.Value,
            result.AssetContentId!.Value,
            content?.ContentHash ?? string.Empty,
            wasDeduped));
    }
}
