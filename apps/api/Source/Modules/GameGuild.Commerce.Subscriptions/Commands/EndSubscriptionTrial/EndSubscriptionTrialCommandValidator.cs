using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for EndSubscriptionTrialCommand
/// </summary>
public class EndSubscriptionTrialCommandValidator : AbstractValidator<EndSubscriptionTrialCommand>
{
    public EndSubscriptionTrialCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
