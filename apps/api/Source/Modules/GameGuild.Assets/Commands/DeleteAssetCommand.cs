using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Commands;

/// <summary>
/// Command to delete an asset reference.
/// </summary>
public record DeleteAssetCommand(
    Guid AssetReferenceId,
    Guid UserId,
    bool ForceDelete = false) : IRequest<Result<DeleteAssetResponse>>;

public record DeleteAssetResponse(
    bool Success,
    bool ContentMarkedForDeletion);

public class DeleteAssetValidator : AbstractValidator<DeleteAssetCommand>
{
    public DeleteAssetValidator()
    {
        RuleFor(x => x.AssetReferenceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class DeleteAssetHandler : IRequestHandler<DeleteAssetCommand, Result<DeleteAssetResponse>>
{
    private readonly IAssetReferenceRepository _referenceRepository;
    private readonly IAssetContentRepository _contentRepository;

    public DeleteAssetHandler(
        IAssetReferenceRepository referenceRepository,
        IAssetContentRepository contentRepository)
    {
        _referenceRepository = referenceRepository;
        _contentRepository = contentRepository;
    }

    public async Task<Result<DeleteAssetResponse>> HandleAsync(
        DeleteAssetCommand request,
        CancellationToken ct = default)
    {
        var reference = await _referenceRepository.GetByIdAsync(request.AssetReferenceId, ct);
        if (reference == null)
        {
            return Result<DeleteAssetResponse>.Failure("Asset not found");
        }

        // Verify ownership (admin override with ForceDelete)
        if (!request.ForceDelete && 
            !await _referenceRepository.IsOwnedByUserAsync(request.AssetReferenceId, request.UserId, ct))
        {
            return Result<DeleteAssetResponse>.Failure("Access denied. You do not own this asset.");
        }

        var contentId = reference.AssetContentId;

        // Soft delete the reference
        await _referenceRepository.DeleteAsync(request.AssetReferenceId, ct);

        // Decrement content reference count (may mark for garbage collection)
        await _contentRepository.DecrementReferenceCountAsync(contentId, ct);

        // Check if content is now marked for deletion
        var content = await _contentRepository.GetByIdAsync(contentId, ct);
        var contentMarkedForDeletion = content?.MarkedForDeletionAt != null;

        return Result<DeleteAssetResponse>.Success(new DeleteAssetResponse(
            true,
            contentMarkedForDeletion));
    }
}
