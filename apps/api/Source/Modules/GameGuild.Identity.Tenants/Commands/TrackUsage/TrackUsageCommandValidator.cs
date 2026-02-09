using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for TrackUsageCommand
/// </summary>
public sealed class TrackUsageCommandValidator : AbstractValidator<TrackUsageCommand>
{
    public TrackUsageCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.ResourceType).NotEmpty().WithMessage("Resource type is required").MaximumLength(100).WithMessage("Resource type must not exceed 100 characters");

        RuleFor(x => x.ActionType).NotEmpty().WithMessage("Action type is required").MaximumLength(100).WithMessage("Action type must not exceed 100 characters");

        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue).WithMessage("Cost must be greater than or equal to 0");
    }
}
