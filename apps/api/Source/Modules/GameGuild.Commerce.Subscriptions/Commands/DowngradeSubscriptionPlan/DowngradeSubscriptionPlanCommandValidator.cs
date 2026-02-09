using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for DowngradeSubscriptionPlanCommand
/// </summary>
public sealed class DowngradeSubscriptionPlanCommandValidator : AbstractValidator<DowngradeSubscriptionPlanCommand>
{
    public DowngradeSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.NewPlanId).NotEmpty().WithMessage("NewPlanId is required");

        RuleFor(x => x.EffectiveDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date).When(x => x.EffectiveDate.HasValue).WithMessage("EffectiveDate must be today or in the future when specified");
    }
}
