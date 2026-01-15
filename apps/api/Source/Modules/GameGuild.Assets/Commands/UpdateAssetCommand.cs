using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Commands;

/// <summary>
/// Command to update an asset reference.
/// </summary>
public record UpdateAssetCommand(
    Guid AssetReferenceId,
    Guid UserId,
    string? DisplayName = null,
    AssetAccessPolicy? AccessPolicy = null) : IRequest<Result<UpdateAssetResponse>>;

public record UpdateAssetResponse(
    Guid AssetReferenceId,
    string DisplayName,
    AssetAccessPolicy AccessPolicy);

public class UpdateAssetValidator : AbstractValidator<UpdateAssetCommand>
{
    public UpdateAssetValidator()
    {
        RuleFor(x => x.AssetReferenceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DisplayName).MaximumLength(255).When(x => x.DisplayName != null);
    }
}

public class UpdateAssetHandler : IRequestHandler<UpdateAssetCommand, Result<UpdateAssetResponse>>
{
    private readonly IAssetReferenceRepository _referenceRepository;

    public UpdateAssetHandler(IAssetReferenceRepository referenceRepository)
    {
        _referenceRepository = referenceRepository;
    }

    public async Task<Result<UpdateAssetResponse>> HandleAsync(
        UpdateAssetCommand request,
        CancellationToken ct = default)
    {
        var reference = await _referenceRepository.GetByIdAsync(request.AssetReferenceId, ct);
        if (reference == null)
        {
            return Result<UpdateAssetResponse>.Failure("Asset not found");
        }

        // Verify ownership
        if (!await _referenceRepository.IsOwnedByUserAsync(request.AssetReferenceId, request.UserId, ct))
        {
            return Result<UpdateAssetResponse>.Failure("Access denied. You do not own this asset.");
        }

        // Apply updates
        if (request.DisplayName != null)
        {
            reference.UpdateDisplayName(request.DisplayName);
        }

        if (request.AccessPolicy.HasValue)
        {
            reference.UpdateAccessPolicy(request.AccessPolicy.Value);
        }

        await _referenceRepository.UpdateAsync(reference, ct);

        return Result<UpdateAssetResponse>.Success(new UpdateAssetResponse(
            reference.Id,
            reference.DisplayName,
            reference.AccessPolicy));
    }
}
