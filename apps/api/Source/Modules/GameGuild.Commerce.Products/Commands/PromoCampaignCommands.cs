using FluentValidation;
using GameGuild.CQRS;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Thrown when a promo campaign is not found by id (issue #418).
/// </summary>
public sealed class PromoCampaignNotFoundException(Guid campaignId)
    : Exception($"Promo campaign with ID '{campaignId}' was not found.")
{
    /// <summary>The missing campaign id.</summary>
    public Guid CampaignId { get; } = campaignId;
}

/// <summary>
/// Command to create a promotional campaign (issue #418 campaign management).
/// </summary>
/// <param name="Name">Display name</param>
/// <param name="Key">The grouping key promo codes reference as CampaignId (tenant-unique)</param>
/// <param name="Description">What the campaign is about</param>
/// <param name="StartsAt">Window start in the campaign timezone (null = unbounded)</param>
/// <param name="EndsAt">Window end in the campaign timezone (null = unbounded)</param>
/// <param name="TimeZoneId">IANA timezone the window is expressed in (null = tenant default)</param>
/// <param name="Budget">Planned spend</param>
/// <param name="AcquisitionSpend">Acquisition spend; CAC numerator</param>
/// <param name="Currency">Currency of the spend fields</param>
/// <param name="CreatedBy">Creating user</param>
public sealed record CreatePromoCampaignCommand(
    string Name,
    string Key,
    string? Description = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? TimeZoneId = null,
    decimal? Budget = null,
    decimal? AcquisitionSpend = null,
    string Currency = "USD",
    Guid? CreatedBy = null
) : ICommand<PromoCampaignDto>;

/// <summary>Handler for <see cref="CreatePromoCampaignCommand"/>.</summary>
public sealed class CreatePromoCampaignCommandHandler(
    IPromoCampaignRepository campaignRepository,
    IPromoCodeAuditLogger auditLogger,
    IOptions<PromoEngineOptions>? engineOptions = null)
    : ICommandHandler<CreatePromoCampaignCommand, PromoCampaignDto>
{
    /// <inheritdoc />
    public async Task<PromoCampaignDto> Handle(CreatePromoCampaignCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = request.Key.Trim();
        if (await campaignRepository.KeyExistsAsync(key, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"A promo campaign with key '{key}' already exists.");
        }

        var (timeZone, _) = PromoValidityWindow.ResolveTimeZone(
            string.IsNullOrWhiteSpace(request.TimeZoneId) ? engineOptions?.Value.DefaultTimeZoneId : request.TimeZoneId);

        var campaign = new PromoCampaign
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Key = key,
            Description = request.Description,
            StartsAtUtc = PromoValidityWindow.NormalizeToUtc(request.StartsAt, timeZone),
            EndsAtUtc = PromoValidityWindow.NormalizeToUtc(request.EndsAt, timeZone),
            TimeZoneId = timeZone.Id,
            Budget = request.Budget,
            AcquisitionSpend = request.AcquisitionSpend,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.ToUpperInvariant(),
            CreatedBy = request.CreatedBy ?? Guid.Empty
        };

        await campaignRepository.AddAsync(campaign, cancellationToken).ConfigureAwait(false);
        await campaignRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditLogger.LogMutationAsync(
            request.CreatedBy,
            "CampaignCreated",
            campaign.Id,
            campaign.Key,
            new { campaignKey = campaign.Key, name = campaign.Name },
            cancellationToken).ConfigureAwait(false);

        return campaign.ToDto(
            codeCount: 0,
            attributedCustomers: 0);
    }
}

/// <summary>Validator for <see cref="CreatePromoCampaignCommand"/>.</summary>
public sealed class CreatePromoCampaignCommandValidator : AbstractValidator<CreatePromoCampaignCommand>
{
    /// <summary>Initializes the validator.</summary>
    public CreatePromoCampaignCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campaign name is required.")
            .MaximumLength(200).WithMessage("Campaign name cannot exceed 200 characters.");

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Campaign key is required.")
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]{1,99}$")
            .WithMessage("Campaign key must be 2-100 characters: letters, digits, dots, underscores or hyphens, starting with a letter or digit.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.TimeZoneId)
            .Must(timeZoneId => PromoValidityWindow.IsKnownTimeZone(timeZoneId))
            .WithMessage("TimeZoneId must be a resolvable IANA timezone id (e.g. America/Sao_Paulo).")
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZoneId));

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Campaign end must be after its start.")
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue);

        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0).WithMessage("Budget cannot be negative.")
            .When(x => x.Budget.HasValue);

        RuleFor(x => x.AcquisitionSpend)
            .GreaterThanOrEqualTo(0).WithMessage("Acquisition spend cannot be negative.")
            .When(x => x.AcquisitionSpend.HasValue);

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency code must be exactly 3 characters.");
    }
}

/// <summary>
/// Command to update a promotional campaign (issue #418). The key is immutable;
/// the lifecycle status moves only through the dedicated status commands.
/// </summary>
public sealed record UpdatePromoCampaignCommand(
    Guid CampaignId,
    string? Name = null,
    string? Description = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? TimeZoneId = null,
    decimal? Budget = null,
    decimal? AcquisitionSpend = null,
    string? Currency = null,
    Guid? UpdatedBy = null
) : ICommand<PromoCampaignDto>;

/// <summary>Handler for <see cref="UpdatePromoCampaignCommand"/>.</summary>
public sealed class UpdatePromoCampaignCommandHandler(
    IPromoCampaignRepository campaignRepository,
    IPromoCodeAuditLogger auditLogger,
    IOptions<PromoEngineOptions>? engineOptions = null)
    : ICommandHandler<UpdatePromoCampaignCommand, PromoCampaignDto>
{
    /// <inheritdoc />
    public async Task<PromoCampaignDto> Handle(UpdatePromoCampaignCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken).ConfigureAwait(false)
            ?? throw new PromoCampaignNotFoundException(request.CampaignId);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            campaign.Name = request.Name;
        }

        if (request.Description != null)
        {
            campaign.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        }

        if (request.Budget.HasValue)
        {
            campaign.Budget = request.Budget.Value;
        }

        if (request.AcquisitionSpend.HasValue)
        {
            campaign.AcquisitionSpend = request.AcquisitionSpend.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            campaign.Currency = request.Currency.ToUpperInvariant();
        }

        if (request.StartsAt.HasValue || request.EndsAt.HasValue || !string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            var defaultTimeZoneId = engineOptions?.Value.DefaultTimeZoneId;
            var effectiveTimeZoneId = !string.IsNullOrWhiteSpace(request.TimeZoneId)
                ? request.TimeZoneId
                : !string.IsNullOrWhiteSpace(campaign.TimeZoneId) ? campaign.TimeZoneId : defaultTimeZoneId;
            var (timeZone, _) = PromoValidityWindow.ResolveTimeZone(effectiveTimeZoneId);

            if (request.StartsAt.HasValue)
            {
                campaign.StartsAtUtc = PromoValidityWindow.NormalizeToUtc(request.StartsAt, timeZone);
            }

            if (request.EndsAt.HasValue)
            {
                campaign.EndsAtUtc = PromoValidityWindow.NormalizeToUtc(request.EndsAt, timeZone);
            }

            campaign.TimeZoneId = timeZone.Id;
        }

        campaign.Touch();
        await campaignRepository.UpdateAsync(campaign, cancellationToken).ConfigureAwait(false);
        await campaignRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditLogger.LogMutationAsync(
            request.UpdatedBy,
            "CampaignUpdated",
            campaign.Id,
            campaign.Key,
            new { campaignKey = campaign.Key },
            cancellationToken).ConfigureAwait(false);

        var codeCount = await campaignRepository.CountCodesForKeyAsync(campaign.Key, cancellationToken).ConfigureAwait(false);
        var attributed = await campaignRepository.CountAttributionsForKeyAsync(campaign.Key, cancellationToken).ConfigureAwait(false);
        return campaign.ToDto(codeCount, attributed);
    }
}

/// <summary>Validator for <see cref="UpdatePromoCampaignCommand"/>.</summary>
public sealed class UpdatePromoCampaignCommandValidator : AbstractValidator<UpdatePromoCampaignCommand>
{
    /// <summary>Initializes the validator.</summary>
    public UpdatePromoCampaignCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campaign name cannot be empty when supplied.")
            .MaximumLength(200).WithMessage("Campaign name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.TimeZoneId)
            .Must(timeZoneId => PromoValidityWindow.IsKnownTimeZone(timeZoneId))
            .WithMessage("TimeZoneId must be a resolvable IANA timezone id.")
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZoneId));

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Campaign end must be after its start.")
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue);

        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0).WithMessage("Budget cannot be negative.")
            .When(x => x.Budget.HasValue);

        RuleFor(x => x.AcquisitionSpend)
            .GreaterThanOrEqualTo(0).WithMessage("Acquisition spend cannot be negative.")
            .When(x => x.AcquisitionSpend.HasValue);

        RuleFor(x => x.Currency)
            .Length(3).WithMessage("Currency code must be exactly 3 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
    }
}

/// <summary>Activates a draft or paused campaign (issue #418 lifecycle).</summary>
public sealed record ActivatePromoCampaignCommand(Guid CampaignId) : ICommand<PromoCampaignDto>;

/// <summary>Pauses an active campaign (issue #418 lifecycle).</summary>
public sealed record PausePromoCampaignCommand(Guid CampaignId) : ICommand<PromoCampaignDto>;

/// <summary>Completes an active or paused campaign (issue #418 lifecycle).</summary>
public sealed record CompletePromoCampaignCommand(Guid CampaignId) : ICommand<PromoCampaignDto>;

/// <summary>Cancels a draft, active, or paused campaign (issue #418 lifecycle).</summary>
public sealed record CancelPromoCampaignCommand(Guid CampaignId) : ICommand<PromoCampaignDto>;

/// <summary>Archives a completed or cancelled campaign (issue #418 lifecycle).</summary>
public sealed record ArchivePromoCampaignCommand(Guid CampaignId) : ICommand<PromoCampaignDto>;

/// <summary>
/// Shared handler core for campaign lifecycle transitions: loads the campaign, applies the
/// domain transition (invalid transitions throw with the allowed source statuses), saves,
/// audits, and returns the refreshed DTO.
/// </summary>
public static class PromoCampaignStatusTransition
{
    /// <summary>Applies a lifecycle transition and returns the updated DTO.</summary>
    public static async Task<PromoCampaignDto> ApplyAsync(
        IPromoCampaignRepository campaignRepository,
        IPromoCodeAuditLogger auditLogger,
        Guid campaignId,
        string action,
        Action<PromoCampaign> transition,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken).ConfigureAwait(false)
            ?? throw new PromoCampaignNotFoundException(campaignId);

        transition(campaign);
        campaign.Touch();

        await campaignRepository.UpdateAsync(campaign, cancellationToken).ConfigureAwait(false);
        await campaignRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditLogger.LogMutationAsync(
            actorUserId,
            $"Campaign{action}",
            campaign.Id,
            campaign.Key,
            new { campaignKey = campaign.Key, status = campaign.Status.ToString() },
            cancellationToken).ConfigureAwait(false);

        var codeCount = await campaignRepository.CountCodesForKeyAsync(campaign.Key, cancellationToken).ConfigureAwait(false);
        var attributed = await campaignRepository.CountAttributionsForKeyAsync(campaign.Key, cancellationToken).ConfigureAwait(false);
        return campaign.ToDto(codeCount, attributed);
    }
}

/// <summary>Handler for <see cref="ActivatePromoCampaignCommand"/>.</summary>
public sealed class ActivatePromoCampaignCommandHandler(IPromoCampaignRepository campaignRepository, IPromoCodeAuditLogger auditLogger)
    : ICommandHandler<ActivatePromoCampaignCommand, PromoCampaignDto>
{
    /// <inheritdoc />
    public Task<PromoCampaignDto> Handle(ActivatePromoCampaignCommand request, CancellationToken cancellationToken)
        => PromoCampaignStatusTransition.ApplyAsync(
            campaignRepository, auditLogger, request.CampaignId, "Activated",
            campaign => campaign.Activate(), null, cancellationToken);
}

/// <summary>Handler for <see cref="PausePromoCampaignCommand"/>.</summary>
public sealed class PausePromoCampaignCommandHandler(IPromoCampaignRepository campaignRepository, IPromoCodeAuditLogger auditLogger)
    : ICommandHandler<PausePromoCampaignCommand, PromoCampaignDto>
{
    /// <inheritdoc />
    public Task<PromoCampaignDto> Handle(PausePromoCampaignCommand request, CancellationToken cancellationToken)
        => PromoCampaignStatusTransition.ApplyAsync(
            campaignRepository, auditLogger, request.CampaignId, "Paused",
            campaign => campaign.Pause(), null, cancellationToken);
}

/// <summary>Handler for <see cref="CompletePromoCampaignCommand"/>.</summary>
public sealed class CompletePromoCampaignCommandHandler(IPromoCampaignRepository campaignRepository, IPromoCodeAuditLogger auditLogger)
    : ICommandHandler<CompletePromoCampaignCommand, PromoCampaignDto>
{
    /// <inheritdoc />
    public Task<PromoCampaignDto> Handle(CompletePromoCampaignCommand request, CancellationToken cancellationToken)
        => PromoCampaignStatusTransition.ApplyAsync(
            campaignRepository, auditLogger, request.CampaignId, "Completed",
            campaign => campaign.Complete(), null, cancellationToken);
}

/// <summary>Handler for <see cref="CancelPromoCampaignCommand"/>.</summary>
public sealed class CancelPromoCampaignCommandHandler(IPromoCampaignRepository campaignRepository, IPromoCodeAuditLogger auditLogger)
    : ICommandHandler<CancelPromoCampaignCommand, PromoCampaignDto>
{
    /// <inheritdoc />
    public Task<PromoCampaignDto> Handle(CancelPromoCampaignCommand request, CancellationToken cancellationToken)
        => PromoCampaignStatusTransition.ApplyAsync(
            campaignRepository, auditLogger, request.CampaignId, "Cancelled",
            campaign => campaign.Cancel(), null, cancellationToken);
}

/// <summary>Handler for <see cref="ArchivePromoCampaignCommand"/>.</summary>
public sealed class ArchivePromoCampaignCommandHandler(IPromoCampaignRepository campaignRepository, IPromoCodeAuditLogger auditLogger)
    : ICommandHandler<ArchivePromoCampaignCommand, PromoCampaignDto>
{
    /// <inheritdoc />
    public Task<PromoCampaignDto> Handle(ArchivePromoCampaignCommand request, CancellationToken cancellationToken)
        => PromoCampaignStatusTransition.ApplyAsync(
            campaignRepository, auditLogger, request.CampaignId, "Archived",
            campaign => campaign.Archive(), null, cancellationToken);
}

/// <summary>
/// Command to delete a promotional campaign (issue #418). Fails closed while any
/// promo code still references the campaign key — delete or re-scope the codes first.
/// </summary>
public sealed record DeletePromoCampaignCommand(Guid CampaignId, Guid? DeletedBy = null) : ICommand<bool>;

/// <summary>Handler for <see cref="DeletePromoCampaignCommand"/>.</summary>
public sealed class DeletePromoCampaignCommandHandler(
    IPromoCampaignRepository campaignRepository,
    IPromoCodeAuditLogger auditLogger)
    : ICommandHandler<DeletePromoCampaignCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(DeletePromoCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken).ConfigureAwait(false)
            ?? throw new PromoCampaignNotFoundException(request.CampaignId);

        var referencingCodes = await campaignRepository
            .CountCodesForKeyAsync(campaign.Key, cancellationToken)
            .ConfigureAwait(false);
        if (referencingCodes > 0)
        {
            throw new InvalidOperationException(
                $"Promo campaign '{campaign.Key}' still groups {referencingCodes} promo code(s); delete or re-scope the codes before deleting the campaign.");
        }

        await campaignRepository.DeleteAsync(campaign, cancellationToken).ConfigureAwait(false);
        await campaignRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditLogger.LogMutationAsync(
            request.DeletedBy,
            "CampaignDeleted",
            campaign.Id,
            campaign.Key,
            new { campaignKey = campaign.Key },
            cancellationToken).ConfigureAwait(false);

        return true;
    }
}
