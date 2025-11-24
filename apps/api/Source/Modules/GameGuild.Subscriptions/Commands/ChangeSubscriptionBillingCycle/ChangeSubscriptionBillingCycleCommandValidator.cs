using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for ChangeSubscriptionBillingCycleCommand
/// </summary>
public class ChangeSubscriptionBillingCycleCommandValidator : AbstractValidator<ChangeSubscriptionBillingCycleCommand>
{
    public ChangeSubscriptionBillingCycleCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.NewBillingCycle).IsInEnum().WithMessage("NewBillingCycle must be a valid billing cycle value");
    }
}
