using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for StartSubscriptionTrialCommand
/// </summary>
public class StartSubscriptionTrialCommandValidator : AbstractValidator<StartSubscriptionTrialCommand>
{
    public StartSubscriptionTrialCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.TrialDays).GreaterThan(0).WithMessage("TrialDays must be greater than 0").LessThanOrEqualTo(90).WithMessage("TrialDays cannot exceed 90 days");
    }
}
