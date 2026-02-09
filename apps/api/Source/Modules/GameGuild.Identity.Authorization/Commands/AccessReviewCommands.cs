using FluentValidation;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Commands;

// ============================================================================
// Access Review Commands
// ============================================================================

/// <summary>
///     Command to create an access review campaign
/// </summary>
public sealed record CreateAccessReviewCampaignCommand(
    string Name,
    string Description,
    Guid? TenantId,
    AccessReviewType ReviewType,
    DateTime StartDate,
    DateTime EndDate,
    Guid CreatedBy
) : ICommand<AccessReviewCampaign>;

public sealed class CreateAccessReviewCampaignValidator : AbstractValidator<CreateAccessReviewCampaignCommand>
{
    public CreateAccessReviewCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ReviewType).IsInEnum();
        RuleFor(x => x.StartDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}

public sealed class CreateAccessReviewCampaignHandler(IAccessReviewService service)
    : ICommandHandler<CreateAccessReviewCampaignCommand, AccessReviewCampaign>
{
    public async Task<AccessReviewCampaign> Handle(
        CreateAccessReviewCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        var campaign = new AccessReviewCampaign
        {
            Name = request.Name,
            Description = request.Description,
            TenantId = request.TenantId,
            ReviewType = request.ReviewType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = AccessReviewStatus.Draft, CreatedBy = request.CreatedBy
        };

        return await service.CreateCampaignAsync(campaign, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to start an access review campaign
/// </summary>
public sealed record StartAccessReviewCampaignCommand(Guid CampaignId) : ICommand<bool>;

public sealed class StartAccessReviewCampaignValidator : AbstractValidator<StartAccessReviewCampaignCommand>
{
    public StartAccessReviewCampaignValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}

public sealed class StartAccessReviewCampaignHandler(IAccessReviewService service)
    : ICommandHandler<StartAccessReviewCampaignCommand, bool>
{
    public async Task<bool> Handle(
        StartAccessReviewCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.StartCampaignAsync(request.CampaignId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to complete an access review campaign
/// </summary>
public sealed record CompleteAccessReviewCampaignCommand(
    Guid CampaignId,
    Guid CompletedBy
) : ICommand<bool>;

public sealed class CompleteAccessReviewCampaignValidator : AbstractValidator<CompleteAccessReviewCampaignCommand>
{
    public CompleteAccessReviewCampaignValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.CompletedBy).NotEmpty();
    }
}

public sealed class CompleteAccessReviewCampaignHandler(IAccessReviewService service)
    : ICommandHandler<CompleteAccessReviewCampaignCommand, bool>
{
    public async Task<bool> Handle(
        CompleteAccessReviewCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.CompleteCampaignAsync(
            request.CampaignId,
            request.CompletedBy,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to cancel an access review campaign
/// </summary>
public sealed record CancelAccessReviewCampaignCommand(Guid CampaignId) : ICommand<bool>;

public sealed class CancelAccessReviewCampaignValidator : AbstractValidator<CancelAccessReviewCampaignCommand>
{
    public CancelAccessReviewCampaignValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}

public sealed class CancelAccessReviewCampaignHandler(IAccessReviewService service)
    : ICommandHandler<CancelAccessReviewCampaignCommand, bool>
{
    public async Task<bool> Handle(
        CancelAccessReviewCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.CancelCampaignAsync(request.CampaignId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to approve an access review item
/// </summary>
public sealed record ApproveAccessReviewItemCommand(
    Guid ItemId,
    string? Reason = null,
    string? Notes = null
) : ICommand<AccessReviewItem>;

public sealed class ApproveAccessReviewItemValidator : AbstractValidator<ApproveAccessReviewItemCommand>
{
    public ApproveAccessReviewItemValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => x.Reason != null);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}

public sealed class ApproveAccessReviewItemHandler(IAccessReviewService service)
    : ICommandHandler<ApproveAccessReviewItemCommand, AccessReviewItem>
{
    public async Task<AccessReviewItem> Handle(
        ApproveAccessReviewItemCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.ApproveItemAsync(
            request.ItemId,
            request.Reason,
            request.Notes,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to revoke access for an access review item
/// </summary>
public sealed record RevokeAccessReviewItemCommand(
    Guid ItemId,
    string Reason,
    string? Notes = null
) : ICommand<AccessReviewItem>;

public sealed class RevokeAccessReviewItemValidator : AbstractValidator<RevokeAccessReviewItemCommand>
{
    public RevokeAccessReviewItemValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}

public sealed class RevokeAccessReviewItemHandler(IAccessReviewService service)
    : ICommandHandler<RevokeAccessReviewItemCommand, AccessReviewItem>
{
    public async Task<AccessReviewItem> Handle(
        RevokeAccessReviewItemCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.RevokeItemAsync(
            request.ItemId,
            request.Reason,
            request.Notes,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to send reminders for a campaign
/// </summary>
public sealed record SendAccessReviewRemindersCommand(Guid CampaignId) : ICommand<int>;

public sealed class SendAccessReviewRemindersHandler(IAccessReviewService service)
    : ICommandHandler<SendAccessReviewRemindersCommand, int>
{
    public async Task<int> Handle(
        SendAccessReviewRemindersCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.SendRemindersAsync(request.CampaignId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Command to process expired campaigns
/// </summary>
public sealed record ProcessExpiredCampaignsCommand : ICommand<int>;

public sealed class ProcessExpiredCampaignsHandler(IAccessReviewService service)
    : ICommandHandler<ProcessExpiredCampaignsCommand, int>
{
    public async Task<int> Handle(
        ProcessExpiredCampaignsCommand request,
        CancellationToken cancellationToken
    )
    {
        return await service.ProcessExpiredCampaignsAsync(cancellationToken).ConfigureAwait(false);
    }
}
