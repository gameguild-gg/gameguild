using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Commands;

/// <summary>
/// Command to generate an access URL for an asset.
/// </summary>
public record GenerateAccessUrlCommand(
    Guid AssetReferenceId,
    Guid? UserId,
    Guid? TenantId,
    TransformationSpec? Transformation = null,
    bool DirectStorageUrl = false) : IRequest<GenerateAccessUrlResponse?>;

public record GenerateAccessUrlResponse(
    string Url,
    string? Token,
    DateTimeOffset ExpiresAt,
    string MimeType);

public class GenerateAccessUrlValidator : AbstractValidator<GenerateAccessUrlCommand>
{
    public GenerateAccessUrlValidator()
    {
        RuleFor(x => x.AssetReferenceId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class GenerateAccessUrlHandler : IRequestHandler<GenerateAccessUrlCommand, GenerateAccessUrlResponse?>
{
    private readonly IAssetAccessService _accessService;

    public GenerateAccessUrlHandler(IAssetAccessService accessService)
    {
        _accessService = accessService;
    }

    public async Task<GenerateAccessUrlResponse?> Handle(
        GenerateAccessUrlCommand request,
        CancellationToken ct = default)
    {
        AssetAccessUrl? accessUrl;

        if (request.DirectStorageUrl)
        {
            accessUrl = await _accessService.GenerateDirectStorageUrlAsync(
                request.AssetReferenceId,
                request.UserId,
                request.TenantId,
                ct);
        }
        else
        {
            accessUrl = await _accessService.GenerateAccessUrlAsync(
                request.AssetReferenceId,
                request.UserId,
                request.TenantId,
                request.Transformation,
                ct);
        }

        if (accessUrl == null)
        {
            return null;
        }

        return new GenerateAccessUrlResponse(
            accessUrl.Url,
            string.IsNullOrEmpty(accessUrl.Token) ? null : accessUrl.Token,
            accessUrl.ExpiresAt,
            accessUrl.MimeType);
    }
}
