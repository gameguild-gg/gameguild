using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for UpgradeSubscriptionPlanCommand
/// </summary>
public sealed class UpgradeSubscriptionPlanCommandValidator : AbstractValidator<UpgradeSubscriptionPlanCommand>
{
    public UpgradeSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.NewPlanId).NotEmpty().WithMessage("NewPlanId is required");

        RuleFor(x => x.EffectiveDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date).When(x => x.EffectiveDate.HasValue).WithMessage("EffectiveDate must be today or in the future when specified");
    }
}
