using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for CreateSubscriptionCommand
/// </summary>
public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required");

        RuleFor(x => x.PlanId).NotEmpty().WithMessage("PlanId is required");

        RuleFor(x => x.CreatedByUserId).NotEmpty().WithMessage("CreatedByUserId is required");

        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithMessage("Amount must be greater than or equal to 0");

        RuleFor(x => x.TrialDays).GreaterThan(0).When(x => x.TrialDays.HasValue).WithMessage("TrialDays must be greater than 0 when specified");

        RuleFor(x => x.StartDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date).When(x => x.StartDate.HasValue).WithMessage("StartDate must be today or in the future when specified");
    }
}
