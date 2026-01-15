using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Commands;

/// <summary>
/// Command to report an asset for moderation.
/// </summary>
public record ReportAssetCommand(
    Guid AssetReferenceId,
    Guid ReportedByUserId,
    ReportReason Reason,
    string? Description = null) : IRequest<Result<ReportAssetResponse>>;

public record ReportAssetResponse(
    Guid ReportId,
    ReportStatus Status);

public class ReportAssetValidator : AbstractValidator<ReportAssetCommand>
{
    public ReportAssetValidator()
    {
        RuleFor(x => x.AssetReferenceId).NotEmpty();
        RuleFor(x => x.ReportedByUserId).NotEmpty();
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
    }
}

public class ReportAssetHandler : IRequestHandler<ReportAssetCommand, Result<ReportAssetResponse>>
{
    private readonly IAssetModerationService _moderationService;
    private readonly IAssetReferenceRepository _referenceRepository;

    public ReportAssetHandler(
        IAssetModerationService moderationService,
        IAssetReferenceRepository referenceRepository)
    {
        _moderationService = moderationService;
        _referenceRepository = referenceRepository;
    }

    public async Task<Result<ReportAssetResponse>> HandleAsync(
        ReportAssetCommand request,
        CancellationToken ct = default)
    {
        // Verify asset exists
        var reference = await _referenceRepository.GetByIdAsync(request.AssetReferenceId, ct);
        if (reference == null)
        {
            return Result<ReportAssetResponse>.Failure("Asset not found");
        }

        // Cannot report your own asset
        if (reference.CreatedByUserId == request.ReportedByUserId)
        {
            return Result<ReportAssetResponse>.Failure("You cannot report your own asset");
        }

        var report = await _moderationService.CreateReportAsync(
            request.AssetReferenceId,
            request.ReportedByUserId,
            request.Reason,
            request.Description,
            ct);

        if (report == null)
        {
            return Result<ReportAssetResponse>.Failure("You have already reported this asset");
        }

        return Result<ReportAssetResponse>.Success(new ReportAssetResponse(
            report.Id,
            report.Status));
    }
}
