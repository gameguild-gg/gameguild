using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for UpdateSubscriptionPlanPricingCommand
/// </summary>
public class UpdateSubscriptionPlanPricingCommandValidator : AbstractValidator<UpdateSubscriptionPlanPricingCommand>
{
    public UpdateSubscriptionPlanPricingCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
