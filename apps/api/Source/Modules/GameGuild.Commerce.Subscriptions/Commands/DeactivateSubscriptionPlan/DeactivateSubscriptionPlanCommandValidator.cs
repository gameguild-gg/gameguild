using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for DeactivateSubscriptionPlanCommand
/// </summary>
public class DeactivateSubscriptionPlanCommandValidator : AbstractValidator<DeactivateSubscriptionPlanCommand>
{
    public DeactivateSubscriptionPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
