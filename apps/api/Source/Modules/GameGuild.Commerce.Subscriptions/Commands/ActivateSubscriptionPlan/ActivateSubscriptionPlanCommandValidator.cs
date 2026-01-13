using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for ActivateSubscriptionPlanCommand
/// </summary>
public class ActivateSubscriptionPlanCommandValidator : AbstractValidator<ActivateSubscriptionPlanCommand>
{
    public ActivateSubscriptionPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
